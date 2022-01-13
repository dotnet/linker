// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (23);
			for (int i = (int) DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter;
				i <= (int) DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter; i++) {
				diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
			}

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

			context.RegisterSymbolAction (context => {
				ProcessGenericTypes (context);
			}, SymbolKind.NamedType);
		}

		static void ProcessGenericTypes (SymbolAnalysisContext context)
		{
			// Check the Symbol to see the generics relationship, specifically around DAMT
			// We will initially explore if an unbounded generic type has mismatched annotations with its base-class,
			// which is an open generic type. i.e. Bar<T> : Foo<[DAMT] T>

			INamedTypeSymbol derivedType = (INamedTypeSymbol) context.Symbol;

			if (derivedType.HasAttribute (RequiresUnreferencedCodeAnalyzer.RequiresUnreferencedCodeAttribute))
				return;

			var baseType = derivedType.BaseType;

			//Check if generic parameter of derived and generic argument of the base have mismatched annotation

			var typeParams = baseType!.TypeParameters;
			var typeArgs = baseType.TypeArguments;

			for (int i = 0; i < typeParams.Length; i++) {
				var sourceValue = new SymbolValue (typeArgs[i]);
				var targetValue = new SymbolValue (typeParams[i]);
				foreach (var diagnostic in GetDynamicallyAccessedMembersDiagnostics (sourceValue, targetValue, typeArgs[i].Locations.First ()))
					context.ReportDiagnostic (diagnostic);
			}
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
			// The target should always be an annotated value, but the visitor design currently prevents
			// declaring this in the type system.
			if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
				throw new NotImplementedException ();

			// For now only implement annotated value versus annotated value comparisons. Eventually this method should be replaced by a shared version
			// of the ReflectionMethodBodyScanner.RequireDynamicallyAccessedMembers from the linker
			// which will handle things like constant string/type values, "marking" as appropriate, unknown values, null values, ....
			if (sourceValue is not ValueWithDynamicallyAccessedMembers sourceWithDynamicallyAccessedMembers)
				yield break;

			Debug.Assert (target.Source.Kind is not SymbolKind.NamedType);
			var damtOnTarget = target.DynamicallyAccessedMemberTypes;
			var damtOnSource = source.DynamicallyAccessedMemberTypes;

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				yield break;

			(var diagnosticId, var diagnosticArguments) = Annotations.GetDiagnosticForAnnotationMismatch (sourceWithDynamicallyAccessedMembers, targetWithDynamicallyAccessedMembers, missingAnnotations);

			yield return Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diagnosticId), location, diagnosticArguments);
		}
	}
}
