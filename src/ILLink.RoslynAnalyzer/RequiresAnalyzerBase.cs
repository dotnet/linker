// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public abstract class RequiresAnalyzerBase : DiagnosticAnalyzer
	{
		private protected abstract string RequiresAttributeName { get; }

		private protected abstract string RequiresAttributeFullyQualifiedName { get; }

		private protected abstract DiagnosticTargets AnalyzerDiagnosticTargets { get; }

		private protected abstract DiagnosticDescriptor RequiresDiagnosticRule { get; }

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (VerifyMSBuildOptions (context.Options, compilation) == false)
					return;
				var dangerousPatterns = GetSpecialIncompatibleMembers (compilation);

				context.RegisterOperationAction (operationContext => {
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, methodInvocation.TargetMethod, dangerousPatterns);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var objectCreation = (IObjectCreationOperation) operationContext.Operation;
					var ctor = objectCreation.Constructor;
					if (ctor is not null) {
						CheckCalledMember (operationContext, ctor, dangerousPatterns);
					}
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var fieldAccess = (IFieldReferenceOperation) operationContext.Operation;
					if (fieldAccess.Field.ContainingType is INamedTypeSymbol { StaticConstructors: var ctors } &&
						!SymbolEqualityComparer.Default.Equals (operationContext.ContainingSymbol.ContainingType, fieldAccess.Field.ContainingType)) {
						CheckStaticConstructors (operationContext, ctors);
					}
				}, OperationKind.FieldReference);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckCalledMember (operationContext, prop.GetMethod, dangerousPatterns);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckCalledMember (operationContext, prop.SetMethod, dangerousPatterns);

					if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Property))
						CheckCalledMember (operationContext, prop, dangerousPatterns);
				}, OperationKind.PropertyReference);

				if (AnalyzerDiagnosticTargets.HasFlag (DiagnosticTargets.Event)) {
					context.RegisterOperationAction (operationContext => {
						var eventRef = (IEventReferenceOperation) operationContext.Operation;
						CheckCalledMember (operationContext, eventRef.Member, dangerousPatterns);
					}, OperationKind.EventReference);
				}

				context.RegisterOperationAction (operationContext => {
					var delegateCreation = (IDelegateCreationOperation) operationContext.Operation;
					IMethodSymbol methodSymbol;
					if (delegateCreation.Target is IMethodReferenceOperation methodRef)
						methodSymbol = methodRef.Method;
					else if (delegateCreation.Target is IAnonymousFunctionOperation lambda)
						methodSymbol = lambda.Symbol;
					else
						return;
					CheckCalledMember (operationContext, methodSymbol, dangerousPatterns);
				}, OperationKind.DelegateCreation);

				void CheckStaticConstructors (OperationAnalysisContext operationContext,
					ImmutableArray<IMethodSymbol> staticConstructors)
				{
					foreach (var staticConstructor in staticConstructors) {
						if (staticConstructor.HasAttribute (RequiresAttributeName) && TryGetRequiresAttribute (staticConstructor, out AttributeData? requiresAttribute))
							ReportRequiresDiagnostic (operationContext, staticConstructor, requiresAttribute);
					}
				}

				void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member,
					ImmutableArray<ISymbol> dangerousPatterns)
				{
					ISymbol containingSymbol = FindContainingSymbol (operationContext, AnalyzerDiagnosticTargets);

					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (containingSymbol.HasAttribute (RequiresAttributeName))
						return;
					// Check also for RequiresAttribute in the associated symbol
					if (containingSymbol is IMethodSymbol methodSymbol && methodSymbol.AssociatedSymbol is not null && methodSymbol.AssociatedSymbol!.HasAttribute (RequiresAttributeName)) {
						return;
					}
					// If calling an instance constructor, check first for any static constructor since it will be called implicitly
					if (member.ContainingType is { } containingType && operationContext.Operation is IObjectCreationOperation)
						CheckStaticConstructors (operationContext, containingType.StaticConstructors);

					if (ReportSpecialIncompatibleMembersDiagnostic (operationContext, dangerousPatterns, member))
						return;

					if (!member.HasAttribute (RequiresAttributeName))
						return;

					// Warn on the most derived base method taking into account covariant returns
					while (member is IMethodSymbol method && method.OverriddenMethod != null && SymbolEqualityComparer.Default.Equals (method.ReturnType, method.OverriddenMethod.ReturnType))
						member = method.OverriddenMethod;

					if (TryGetRequiresAttribute (member, out AttributeData? requiresAttribute)) {
						ReportRequiresDiagnostic (operationContext, member, requiresAttribute);
					}
				}
			});
		}

		[Flags]
		protected enum DiagnosticTargets
		{
			MethodOrConstructor = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			All = MethodOrConstructor | Property | Field | Event
		}

		private static ISymbol FindContainingSymbol (OperationAnalysisContext operationContext, DiagnosticTargets targets)
		{
			var parent = operationContext.Operation.Parent;
			while (parent is not null) {
				switch (parent) {
				case IAnonymousFunctionOperation lambda:
					return lambda.Symbol;
				case ILocalFunctionOperation local when targets.HasFlag (DiagnosticTargets.MethodOrConstructor):
					return local.Symbol;
				case IMethodBodyBaseOperation when targets.HasFlag (DiagnosticTargets.MethodOrConstructor):
				case IPropertyReferenceOperation when targets.HasFlag (DiagnosticTargets.Property):
				case IFieldReferenceOperation when targets.HasFlag (DiagnosticTargets.Field):
				case IEventReferenceOperation when targets.HasFlag (DiagnosticTargets.Event):
					return operationContext.ContainingSymbol;
				default:
					parent = parent.Parent;
					break;
				}
			}
			return operationContext.ContainingSymbol;
		}

		private void ReportRequiresDiagnostic (OperationAnalysisContext operationContext, ISymbol member, AttributeData? requiresAttribute)
		{
			var message = GetMessageFromAttribute (requiresAttribute);
			var url = GetUrlFromAttribute (requiresAttribute);
			operationContext.ReportDiagnostic (Diagnostic.Create (
				RequiresDiagnosticRule,
				operationContext.Operation.Syntax.GetLocation (),
				member.ToString (),
				message,
				url));
		}

		protected abstract string GetMessageFromAttribute (AttributeData? requiresAssemblyFilesAttribute);

		private string GetUrlFromAttribute (AttributeData? requiresAssemblyFilesAttribute)
		{
			var url = requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
			return string.IsNullOrEmpty (url) ? "" : " " + url;
		}

		private bool TryGetRequiresAttribute (ISymbol member, out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			foreach (var _attribute in member.GetAttributes ()) {
				if (_attribute.AttributeClass is { } attrClass &&
					attrClass.HasName (RequiresAttributeFullyQualifiedName) &&
					VerifyAttributeArguments(_attribute)) {
					requiresAttribute = _attribute;
					return true;
				}
			}
			return false;
		}

		protected abstract bool VerifyAttributeArguments (AttributeData attribute);

		protected abstract bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> specialIncompatibleMembers, ISymbol member);

		protected abstract ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation);

		protected abstract bool VerifyMSBuildOptions (AnalyzerOptions options, Compilation compilation);
	}
}