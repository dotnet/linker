// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode);
		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeAttributeMismatch = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCodeAttributeMismatch);
		static readonly DiagnosticDescriptor s_dynamicTypeInvocationRule = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresUnreferencedCode,
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationTitle), SharedStrings.ResourceManager, typeof (SharedStrings)),
			new LocalizableResourceString (nameof (SharedStrings.DynamicTypeInvocationMessage), SharedStrings.ResourceManager, typeof (SharedStrings)));

		static readonly Action<OperationAnalysisContext> s_dynamicTypeInvocation = operationContext => {
			if (FindContainingSymbol (operationContext, DiagnosticTargets.All) is ISymbol containingSymbol &&
				containingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
				return;

			operationContext.ReportDiagnostic (Diagnostic.Create (s_dynamicTypeInvocationRule,
				operationContext.Operation.Syntax.GetLocation ()));
		};

		static readonly Action<OperationAnalysisContext> s_constructorConstraint = operationContext => {
			var invocationOperation = (IInvocationOperation) operationContext.Operation;
			if (FindContainingSymbol (operationContext, DiagnosticTargets.All) is not ISymbol containingSymbol ||
			containingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
				return;

			var targetMethod = invocationOperation.TargetMethod;
			if (!targetMethod.IsGenericMethod)
				return;

			for (int i = 0; i < targetMethod.TypeParameters.Length; i++) {
				var typeParameter = targetMethod.TypeParameters[i];
				var typeArgument = targetMethod.TypeArguments[i];
				if (!typeParameter.HasConstructorConstraint)
					continue;

				var typeArgCtors = ((INamedTypeSymbol) typeArgument).InstanceConstructors;
				foreach (var instanceCtor in typeArgCtors) {
					if (instanceCtor.Arity > 0)
						continue;

					if (instanceCtor.TryGetAttribute (RequiresUnreferencedCodeAttribute, out var requiresUnreferencedCodeAttribute)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_requiresUnreferencedCodeRule,
							operationContext.Operation.Syntax.GetLocation (),
							containingSymbol.GetDisplayName (),
							(string) requiresUnreferencedCodeAttribute.ConstructorArguments[0].Value!,
							GetUrlFromAttribute (requiresUnreferencedCodeAttribute)));
					}
				}
			}
		};

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (s_dynamicTypeInvocationRule, s_requiresUnreferencedCodeRule, s_requiresUnreferencedCodeAttributeMismatch);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => s_requiresUnreferencedCodeAttributeMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation) =>
			options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);

		private protected override ImmutableArray<(Action<OperationAnalysisContext> Action, OperationKind[] OperationKind)> ExtraOperationActions {
			get {
				var diagsBuilder = ImmutableArray.CreateBuilder<(Action<OperationAnalysisContext>, OperationKind[])> ();
				diagsBuilder.Add ((s_dynamicTypeInvocation, new OperationKind[] { OperationKind.DynamicInvocation }));
				diagsBuilder.Add ((s_constructorConstraint, new OperationKind[] { OperationKind.Invocation }));

				return diagsBuilder.ToImmutable ();
			}
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return MessageFormat.FormatRequiresAttributeMessageArg (message);
		}
	}
}
