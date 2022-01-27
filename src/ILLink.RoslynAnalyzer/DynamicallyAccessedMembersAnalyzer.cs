// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);

		static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (26);
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode));
			for (int i = (int) DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter;
				i <= (int) DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter; i++) {
				diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
			}
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));

			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			if (!System.Diagnostics.Debugger.IsAttached)
				context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationBlockAction (context => {
				if (context.OwningSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
					return;

				foreach (var operationBlock in context.OperationBlocks) {
					ControlFlowGraph cfg = context.GetControlFlowGraph (operationBlock);
					TrimDataFlowAnalysis trimDataFlowAnalysis = new (context, cfg);

					foreach (TrimAnalysisPattern trimAnalysisPattern in trimDataFlowAnalysis.ComputeTrimAnalysisPatterns ()) {
						foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (trimAnalysisPattern.Source, trimAnalysisPattern.Target, trimAnalysisPattern.Operation.Syntax.GetLocation ()))
							context.ReportDiagnostic (diagnostic);
					}
				}
			});
			// TODO: fix reporting for generic type substitutions. This should happen not only for method invocations,
			// but for any reference to an instantiated method or type.
			context.RegisterOperationAction (context => {
				var invocationOperation = (IInvocationOperation) context.Operation;
				ProcessInvocationOperation (context, invocationOperation);
			}, OperationKind.Invocation);
		}

		static void ProcessInvocationOperation (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			if (context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
				return;

			ProcessTypeArguments (context, invocationOperation);
		}

		static void ProcessTypeArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			var targetMethod = invocationOperation.TargetMethod;
			if (targetMethod.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
				return;

			for (int i = 0; i < targetMethod.TypeParameters.Length; i++) {
				var sourceValue = GetTypeValueNodeFromGenericArgument (targetMethod.TypeArguments[i]);
				var targetValue = new GenericParameterValue (targetMethod.TypeParameters[i]);
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, targetValue, invocationOperation.Syntax.GetLocation ()))
					context.ReportDiagnostic (diagnostic);
			}
		}

		static SingleValue GetTypeValueNodeFromGenericArgument (ITypeSymbol type)
		{
			return type.Kind switch {
				SymbolKind.TypeParameter => new GenericParameterValue ((ITypeParameterSymbol) type),
				// Technically this should be a new value node type as it's not a System.Type instance representation, but just the generic parameter
				// That said we only use it to perform the dynamically accessed members checks and for that purpose treating it as System.Type is perfectly valid.
				SymbolKind.NamedType => new SystemTypeValue ((INamedTypeSymbol) type),
				SymbolKind.ErrorType => UnknownValue.Instance,
				// What about things like ArrayType or PointerType and so on. Linker treats these as "named types" since it can resolve them to concrete type
				_ => throw new NotImplementedException ()
			};
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (ValueSet<SingleValue> source, ValueSet<SingleValue> target, Location location)
		{
			foreach (var targetValue in target) {
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (source, targetValue, location))
					yield return diagnostic;
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (ValueSet<SingleValue> source, SingleValue target, Location location)
		{
			foreach (var sourceValue in source) {
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, target, location))
					yield return diagnostic;
			}
		}

		static IEnumerable<Diagnostic> GetDynamicallyAccessedMembersDiagnostics (SingleValue sourceValue, SingleValue targetValue, Location location)
		{
			if (sourceValue is SystemTypeValue knownType && targetValue is ValueWithDynamicallyAccessedMembers targetForKnownType) {
				var members = knownType.NamedTypeSymbol.GetDynamicallyAccessedMembers (targetForKnownType.DynamicallyAccessedMemberTypes);
				foreach (var member in members) {
					if (member.TargetHasRequiresUnreferencedCodeAttribute (out var requiresAttributeData)
						&& RequiresUnreferencedCodeUtils.VerifyRequiresUnreferencedCodeAttributeArguments (requiresAttributeData)) {
						if (member is IPropertySymbol property) {
							if (property.GetMethod != null)
								yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, property.GetMethod, location);
							if (property.SetMethod != null)
								yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, property.SetMethod, location);
						} else if (member is IEventSymbol eventSymbol) {
							if(eventSymbol.AddMethod != null)
								yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, eventSymbol.AddMethod, location);
							if (eventSymbol.RemoveMethod != null)
								yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, eventSymbol.RemoveMethod, location);
							if(eventSymbol.RaiseMethod != null)
								yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, eventSymbol.RaiseMethod, location);
						} else
							yield return ReportRequiresUnreferencedCodeDiagnostic (requiresAttributeData, member, location);
					}

					var diagnostics = member switch {
						IMethodSymbol methodSymbol => VerifyMethodSymbolForDiagnostic (methodSymbol, location),
						IPropertySymbol propertySymbol => VerifyPropertySymbolForDiagnostic (propertySymbol, location),
						IFieldSymbol fieldSymbol => VerifyFieldSymbolForDiagnostic (fieldSymbol, location),
						_ => new List<Diagnostic> ()
					};
					foreach (var diagnostic in diagnostics)
						yield return diagnostic;
				}
			}

			// The target should always be an annotated value, but the visitor design currently prevents
			// declaring this in the type system.
			if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
				throw new NotImplementedException ();

			// For now only implement annotated value versus annotated value comparisons. Eventually this method should be replaced by a shared version
			// of the ReflectionMethodBodyScanner.RequireDynamicallyAccessedMembers from the linker
			// which will handle things like constant string/type values, "marking" as appropriate, unknown values, null values, ....
			if (sourceValue is not ValueWithDynamicallyAccessedMembers sourceWithDynamicallyAccessedMembers)
				yield break;

			var damtOnTarget = targetWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes;
			var damtOnSource = sourceWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes;

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				yield break;

			(var diagnosticId, var diagnosticArguments) = Annotations.GetDiagnosticForAnnotationMismatch (sourceWithDynamicallyAccessedMembers, targetWithDynamicallyAccessedMembers, missingAnnotations);

			yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diagnosticId), location, diagnosticArguments);
		}

		static Diagnostic ReportRequiresUnreferencedCodeDiagnostic (AttributeData requiresAttributeData, ISymbol member, Location location)
		{
			var message = RequiresUnreferencedCodeUtils.GetMessageFromAttribute (requiresAttributeData);
			var url = RequiresAnalyzerBase.GetUrlFromAttribute (requiresAttributeData);
			return Diagnostic.Create (
				DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode),
				location,
				member.GetDisplayName (),
				message,
				url);
		}

		static IEnumerable<Diagnostic> VerifyMethodSymbolForDiagnostic (IMethodSymbol methodSymbol, Location location)
		{
			foreach (var parameter in methodSymbol.Parameters) {
				if (parameter.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
					yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, methodSymbol.GetDisplayName ());
					yield break;
				}
			}

			if (methodSymbol.IsVirtual && methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None) {
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, methodSymbol.GetDisplayName ());
				yield break;
			}

			if (methodSymbol.IsVirtual && methodSymbol.MethodKind is MethodKind.PropertyGet &&
				(methodSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None ||
				methodSymbol.GetDynamicallyAccessedMemberTypesOnAssociatedSymbol () != DynamicallyAccessedMemberTypes.None)) {
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, methodSymbol.GetDisplayName ());
				yield break;
			}

			if (methodSymbol.MethodKind is MethodKind.PropertySet &&
				(methodSymbol.GetDynamicallyAccessedMemberTypesOnReturnType () != DynamicallyAccessedMemberTypes.None ||
				methodSymbol.GetDynamicallyAccessedMemberTypesOnAssociatedSymbol () != DynamicallyAccessedMemberTypes.None)) {
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, methodSymbol.GetDisplayName ());
				yield break;
			}
		}

		static IEnumerable<Diagnostic> VerifyPropertySymbolForDiagnostic (IPropertySymbol propertySymbol, Location location)
		{
			if (propertySymbol.SetMethod is not null && propertySymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, propertySymbol.SetMethod.GetDisplayName ());
			}

			if (propertySymbol.IsVirtual && propertySymbol.GetMethod is not null && propertySymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None) {
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection), location, propertySymbol.GetMethod.GetDisplayName ());
			}
		}

		static IEnumerable<Diagnostic> VerifyFieldSymbolForDiagnostic (IFieldSymbol fieldSymbol, Location location)
		{
			if (fieldSymbol.GetDynamicallyAccessedMemberTypes () != DynamicallyAccessedMemberTypes.None)
				yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection), location, fieldSymbol.GetDisplayName ());
		}
	}
}
