// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);
		const string FullyQualifiedDynamicallyAccessedMembersAttribute = "System.Diagnostics.CodeAnalysis." + DynamicallyAccessedMembersAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = new DiagnosticDescriptor (
			"IL2041",
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresUnreferencedCodeRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterSyntaxNodeAction (DynamicallyAccessedMembersAnalyze, SyntaxKind.MethodDeclaration);
		}

		static void DynamicallyAccessedMembersAnalyze (SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
		{
			if (syntaxNodeAnalysisContext.Node is not BaseMethodDeclarationSyntax baseMethod)
				return;

			var semanticModel = syntaxNodeAnalysisContext.SemanticModel;
			CheckDynamicallyAccessedMembersOnMethod (baseMethod, semanticModel);
		}

		static void CheckDynamicallyAccessedMembersOnMethod (BaseMethodDeclarationSyntax baseMethod, SemanticModel semanticModel)
		{
			for (int i = 0; i < baseMethod.AttributeLists.Count; i++) {
				AttributeListSyntax attributeList = baseMethod.AttributeLists[i];

				foreach (var attribute in baseMethod.AttributeLists[i].Attributes) {
					ITypeSymbol? attributeType = semanticModel.GetTypeInfo (attribute).Type;
					if (attributeType != null && attributeType.ToDisplayString () != FullyQualifiedDynamicallyAccessedMembersAttribute)
						continue;

					var attributeTarget = attributeList.Target;
					if (attributeTarget == null &&
						baseMethod.GetType () != typeof (Type)) {
						// IL2041: Trim analysis: The 'DynamicallyAccessedMembersAttribute' is not allowed on methods.
						// It is allowed on method return value or method parameters though.

						// DynamicallyAccessedMembersAttribute was put directly on the method itself.
						// This is only allowed for instance methods on System.Type and similar classes.
						// Usually this means the attribute should be placed on the return value of the method or one of the method parameters.
					}

					if (attributeTarget != null && (attributeTarget.Identifier.Kind () == SyntaxKind.ReturnKeyword)) {
						CheckMethodReturnValue (baseMethod, attribute, semanticModel);
						continue;
					}
				}
			}
		}

		static void CheckMethodReturnValue (BaseMethodDeclarationSyntax baseMethod, AttributeSyntax attributeOnReturnValue, SemanticModel semanticModel)
		{
			var returns = new List<ReturnStatementSyntax?> ();
			foreach (var statement in baseMethod.Body?.Statements) {
				if (statement.Kind () == SyntaxKind.ReturnStatement)
					returns.Add (statement as ReturnStatementSyntax);
			}

			var dfAnalysis = semanticModel.AnalyzeDataFlow (baseMethod.Body);
			Debug.Assert (dfAnalysis != null && dfAnalysis.Succeeded);

			var methodParameters = baseMethod.ParameterList.Parameters;
			foreach (var parameter in methodParameters) {
				var parameterSymbol = semanticModel.GetDeclaredSymbol (parameter);
				if (parameterSymbol == null)
					continue;

				dfAnalysis?.DataFlowsOut.Contains (parameterSymbol);
			}

			//var methodParameters = baseMethod.ParameterList.Parameters;
			//foreach (var parameter in methodParameters) {
			//	//foreach (var returnValue in returns) {
			//	//	if (parameter.Identifier.Equals (returnValue)) {
			//	//		if (!HaveMatchingAnnotations (parameter, returnValue)) {
			//	//			// IL2068: Trim analysis: 'target method' method return value does not satisfy 'DynamicallyAccessedMembersAttribute' requirements.
			//	//			// The parameter 'source parameter' of method 'source method' does not have matching annotations.
			//	//			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			//	//		}

			//	//		return;
			//	//	}
			//	//}
			//}


		}

		static bool HaveMatchingAnnotations (SyntaxNode a, SyntaxNode b)
		{
			return false;
		}
	}
}
