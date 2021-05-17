// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);

		#region SupportedDiagnostics
		public const string IL2067 = "IL2067";
		static readonly DiagnosticDescriptor s_IL2067 = new (
			IL2067,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2067),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2068 = "IL2068";
		static readonly DiagnosticDescriptor s_IL2068 = new (
			IL2068,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2068),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2069 = "IL2069";
		static readonly DiagnosticDescriptor s_IL2069 = new (
			IL2069,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2069),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2070 = "IL2070";
		static readonly DiagnosticDescriptor s_IL2070 = new (
			IL2070,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2070),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		// Currently never generated.
		public const string IL2071 = "IL2071";
		static readonly DiagnosticDescriptor s_IL2071 = new (
			IL2071,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2071),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2072 = "IL2072";
		static readonly DiagnosticDescriptor s_IL2072 = new (
			IL2072,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2072),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2073 = "IL2073";
		static readonly DiagnosticDescriptor s_IL2073 = new (
			IL2073,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2073),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2074 = "IL2074";
		static readonly DiagnosticDescriptor s_IL2074 = new (
			IL2074,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2074),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2075 = "IL2075";
		static readonly DiagnosticDescriptor s_IL2075 = new (
			IL2075,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2075),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2076 = "IL2076";
		static readonly DiagnosticDescriptor s_IL2076 = new (
			IL2076,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2076),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2077 = "IL2077";
		static readonly DiagnosticDescriptor s_IL2077 = new (
			IL2077,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2077),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2078 = "IL2078";
		static readonly DiagnosticDescriptor s_IL2078 = new (
			IL2078,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2078),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2079 = "IL2079";
		static readonly DiagnosticDescriptor s_IL2079 = new (
			IL2079,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2079),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2080 = "IL2080";
		static readonly DiagnosticDescriptor s_IL2080 = new (
			IL2080,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2080),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		// Currently never generated.
		public const string IL2081 = "IL2081";
		static readonly DiagnosticDescriptor s_IL2081 = new (
			IL2081,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2081),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2082 = "IL2082";
		static readonly DiagnosticDescriptor s_IL2082 = new (
			IL2082,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2082),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2083 = "IL2083";
		static readonly DiagnosticDescriptor s_IL2083 = new (
			IL2083,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2083),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2084 = "IL2084";
		static readonly DiagnosticDescriptor s_IL2084 = new (
			IL2084,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2084),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2085 = "IL2085";
		static readonly DiagnosticDescriptor s_IL2085 = new (
			IL2085,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2085),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		// Currently never generated.
		public const string IL2086 = "IL2086";
		static readonly DiagnosticDescriptor s_IL2086 = new (
			IL2086,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2086),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2087 = "IL2087";
		static readonly DiagnosticDescriptor s_IL2087 = new (
			IL2087,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2087),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2088 = "IL2088";
		static readonly DiagnosticDescriptor s_IL2088 = new (
			IL2088,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2088),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2089 = "IL2089";
		static readonly DiagnosticDescriptor s_IL2089 = new (
			IL2089,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2089),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2090 = "IL2090";
		static readonly DiagnosticDescriptor s_IL2090 = new (
			IL2090,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2090),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public const string IL2091 = "IL2091";
		static readonly DiagnosticDescriptor s_IL2091 = new (
			IL2091,
			new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersAnnotationsDoNotMatch),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.IL2091),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_IL2067, s_IL2068, s_IL2069,
			s_IL2070, s_IL2071, s_IL2072, s_IL2073, s_IL2074, s_IL2075, s_IL2076, s_IL2077, s_IL2078, s_IL2079,
			s_IL2080, s_IL2081, s_IL2082, s_IL2083, s_IL2084, s_IL2085, s_IL2086, s_IL2087, s_IL2088, s_IL2089,
			s_IL2090, s_IL2091);
		#endregion

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterSyntaxNodeAction (DynamicallyAccessedMembersAnalyze,
				SyntaxKind.InvocationExpression,
				SyntaxKind.ReturnStatement,
				SyntaxKind.SimpleAssignmentExpression);
		}

		static void DynamicallyAccessedMembersAnalyze (SyntaxNodeAnalysisContext context)
		{
			var syntaxNode = context.Node;
			if (syntaxNode.AncestorsAndSelf ().OfType<StatementSyntax> ().FirstOrDefault () is not StatementSyntax statementSyntax)
				return;

			var cfAnalysis = context.SemanticModel.AnalyzeControlFlow (statementSyntax);
			// We don't care about unreachable statements.
			if (cfAnalysis is null || !cfAnalysis.Succeeded || !cfAnalysis.StartPointIsReachable)
				return;

			switch (syntaxNode) {
			case InvocationExpressionSyntax invocationExpression:
				ProcessInvocationExpression (invocationExpression, context);
				break;
			case ReturnStatementSyntax returnStatement:
				ProcessReturnStatement (returnStatement, context);
				break;
			case AssignmentExpressionSyntax assignmentExpression:
				ProcessAssignmentExpression (assignmentExpression, context);
				break;
			}
		}

		static void ProcessInvocationExpression (InvocationExpressionSyntax invocationExpression, SyntaxNodeAnalysisContext context)
		{
			if (context.SemanticModel.GetSymbol (invocationExpression) is not IMethodSymbol invokedMethodSymbol)
				return;

			if (invocationExpression.Expression is MemberAccessExpressionSyntax memberAccessExpression)
				ProcessMemberAccessExpression (memberAccessExpression, invokedMethodSymbol, context);

			// For all generic parameters in the called method, check if the passed type arguments have matching annotations.
			for (int i = 0; i < invokedMethodSymbol.TypeParameters.Length; i++) {
				var typeParameterSymbol = invokedMethodSymbol.TypeParameters[i];
				if (!typeParameterSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnTypeParameter))
					continue;

				var typeArgumentSymbol = invokedMethodSymbol.TypeArguments[i];
				typeArgumentSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnTypeArgument);
				if (!SourceHasMatchingAnnotations (damtOnTypeArgument, damtOnTypeParameter, out var missingAnnotationsOnTypeArgument)) {
					ReportUnmatchedAnnotations (invocationExpression.GetLocation (),
						typeArgumentSymbol, typeParameterSymbol, missingAnnotationsOnTypeArgument, context);
					continue;
				}
			}

			// For all parameters in the called method, check if the passed arguments have matching annotations.
			for (int i = 0; i < invokedMethodSymbol.Parameters.Length; i++) {
				var parameterSymbol = invokedMethodSymbol.Parameters[i];
				if (!parameterSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnParameter))
					continue;

				var argumentSyntaxNode = invocationExpression.ArgumentList.Arguments[i];
				if (context.SemanticModel.GetSymbol (argumentSyntaxNode.Expression) is not ISymbol argumentSymbol)
					continue;

				bool argumentIsAMethodCall = false;
				DynamicallyAccessedMemberTypes? damtOnArgument;
				if (argumentSymbol is IMethodSymbol) {
					argumentSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out damtOnArgument);
					argumentIsAMethodCall = true;
				} else
					argumentSymbol.TryGetDynamicallyAccessedMemberTypes (out damtOnArgument);

				if (argumentSymbol.IsImplicitlyDeclared && argumentSymbol.MetadataName == "this")
					argumentSymbol = argumentSymbol.ContainingSymbol;

				if (!SourceHasMatchingAnnotations (damtOnArgument, damtOnParameter, out var missingAnnotationsOnArgument))
					ReportUnmatchedAnnotations (argumentSyntaxNode.GetLocation (),
						argumentSymbol, parameterSymbol, missingAnnotationsOnArgument, context, argumentIsAMethodCall);
			}
		}

		static void ProcessMemberAccessExpression (MemberAccessExpressionSyntax memberAccessExpression, IMethodSymbol invokedMethodSymbol, SyntaxNodeAnalysisContext context)
		{
			if (context.SemanticModel.GetSymbol (memberAccessExpression.Expression) is not ISymbol memberSymbol ||
				!invokedMethodSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnInvokedMethod))
				return;

			bool memberFromReturnType = false;
			DynamicallyAccessedMemberTypes? damtOnMember;
			if (memberSymbol is IMethodSymbol) {
				memberSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out damtOnMember);
				memberFromReturnType = true;
			} else
				memberSymbol.TryGetDynamicallyAccessedMemberTypes (out damtOnMember);

			if (memberAccessExpression.Expression is ThisExpressionSyntax)
				memberSymbol = memberSymbol.ContainingSymbol;

			if (!SourceHasMatchingAnnotations (damtOnMember, damtOnInvokedMethod, out var missingAnnotationsOnMember))
				ReportUnmatchedAnnotations (memberAccessExpression.GetLocation (),
					memberSymbol, invokedMethodSymbol, missingAnnotationsOnMember, context, memberFromReturnType);
		}

		static void ProcessAssignmentExpression (AssignmentExpressionSyntax assignmentExpression, SyntaxNodeAnalysisContext context)
		{
			if (context.SemanticModel.GetSymbol (assignmentExpression.Left) is not ISymbol fieldSymbol ||
				!fieldSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnField))
				return;

			if (context.SemanticModel.GetSymbol (assignmentExpression.Right) is not ISymbol assignedSymbol)
				return;

			bool assignedValueIsAMethodCall = false;
			DynamicallyAccessedMemberTypes? damtOnAssignedSymbol;
			if (assignedSymbol is IMethodSymbol) {
				assignedSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out damtOnAssignedSymbol);
				assignedValueIsAMethodCall = true;
			} else
				assignedSymbol.TryGetDynamicallyAccessedMemberTypes (out damtOnAssignedSymbol);

			if (assignmentExpression.Right is ThisExpressionSyntax)
				assignedSymbol = assignedSymbol.ContainingSymbol;

			if (!SourceHasMatchingAnnotations (damtOnAssignedSymbol, damtOnField, out var missingAnnotationsOnAssignedValue))
				ReportUnmatchedAnnotations (assignmentExpression.GetLocation (),
					assignedSymbol, fieldSymbol, missingAnnotationsOnAssignedValue, context, sourceIsMethodReturnType: assignedValueIsAMethodCall);
		}

		static void ProcessReturnStatement (ReturnStatementSyntax returnStatement, SyntaxNodeAnalysisContext context)
		{
			if (returnStatement.Expression == null || context.SemanticModel.GetSymbol (returnStatement.Expression) is not ISymbol returnValueSymbol ||
				returnValueSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnReturnValue))
				return;

			bool returnValueIsAMethodCall = returnValueSymbol is IMethodSymbol;
			if (returnStatement.Expression is ThisExpressionSyntax)
				returnValueSymbol = returnValueSymbol.ContainingSymbol;

			var containingMethod = returnStatement.Ancestors ().OfType<MethodDeclarationSyntax> ().First ();
			if (context.SemanticModel.GetDeclaredSymbol (containingMethod) is IMethodSymbol containingMethodSymbol &&
				containingMethodSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out var damtOnReturnType) &&
				!SourceHasMatchingAnnotations (damtOnReturnValue, damtOnReturnType, out var missingAnnotations))
					ReportUnmatchedAnnotations (returnStatement.GetLocation (),
						returnValueSymbol, containingMethodSymbol, missingAnnotations, context, sourceIsMethodReturnType: returnValueIsAMethodCall);
		}

		static bool SourceHasMatchingAnnotations (DynamicallyAccessedMemberTypes? sourceMemberTypes, DynamicallyAccessedMemberTypes? targetMemberTypes, out string missingMemberTypesString)
		{
			missingMemberTypesString = $"'{nameof (DynamicallyAccessedMemberTypes.All)}'";
			if (targetMemberTypes == null)
				return true;

			sourceMemberTypes ??= DynamicallyAccessedMemberTypes.None;
			var missingMemberTypesList = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
				.Cast<DynamicallyAccessedMemberTypes> ()
				.Where (damt => (damt & targetMemberTypes & ~sourceMemberTypes) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (missingMemberTypesList.Count == 0)
				return true;

			if (missingMemberTypesList.Contains (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				missingMemberTypesList.SingleOrDefault (mt => mt == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor) is var ppc &&
				ppc != DynamicallyAccessedMemberTypes.None)
				missingMemberTypesList.Remove (ppc);

			missingMemberTypesString = string.Join (", ", missingMemberTypesList.Select (mmt => $"'DynamicallyAccessedMemberTypes.{mmt}'"));
			return false;
		}

			static void ReportUnmatchedAnnotations (
			Location diagnosticLocation,
			ISymbol sourceSymbol,
			ISymbol targetSymbol,
			string missingMemberTypes,
			SyntaxNodeAnalysisContext context,
			bool sourceIsMethodReturnType = false)
		{
			Debug.Assert (!string.IsNullOrEmpty (missingMemberTypes));

			switch ((sourceSymbol, targetSymbol)) {
			case (IParameterSymbol sourceParameter, IParameterSymbol targetParameter):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2067,
					diagnosticLocation,
					targetParameter.GetDisplayName (),
					targetParameter.ContainingSymbol.GetDisplayName (),
					sourceParameter.GetDisplayName (),
					sourceParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IParameterSymbol sourceParameter, IMethodSymbol targetMethodReturn) when
				diagnosticLocation.SourceTree?.GetRoot ().FindNode (diagnosticLocation.SourceSpan) is ReturnStatementSyntax:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2068,
					diagnosticLocation,
					targetMethodReturn.GetDisplayName (),
					sourceParameter.GetDisplayName (),
					sourceParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IParameterSymbol sourceParameter, IFieldSymbol targetField):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2069,
					diagnosticLocation,
					targetField.GetDisplayName (),
					sourceParameter.GetDisplayName (),
					sourceParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IParameterSymbol sourceParameter, IMethodSymbol targetMethod):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2070,
					diagnosticLocation,
					targetMethod.GetDisplayName (),
					sourceParameter.GetDisplayName (),
					sourceParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			// IL2071 not generated until we have full support of MakeGenericType/MakeGenericMethod

			case (IMethodSymbol sourceMethodReturnType, IParameterSymbol targetParameter) when sourceIsMethodReturnType:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2072,
					diagnosticLocation,
					targetParameter.GetDisplayName (),
					targetParameter.ContainingSymbol.GetDisplayName (),
					sourceMethodReturnType.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethodReturnType, IMethodSymbol targetMethodReturnType) when sourceIsMethodReturnType &&
				diagnosticLocation.SourceTree?.GetRoot ().FindNode (diagnosticLocation.SourceSpan) is ReturnStatementSyntax:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2073,
					diagnosticLocation,
					targetMethodReturnType.GetDisplayName (),
					sourceMethodReturnType.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethodReturnType, IFieldSymbol targetField) when sourceIsMethodReturnType:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2074,
					diagnosticLocation,
					targetField.GetDisplayName (),
					sourceMethodReturnType.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethodReturnType, IMethodSymbol targetMethod) when sourceIsMethodReturnType:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2075,
					diagnosticLocation,
					targetMethod.GetDisplayName (),
					sourceMethodReturnType.GetDisplayName (),
					missingMemberTypes));
				break;
			// IL2076 not generated until we have full support of MakeGenericType/MakeGenericMethod

			case (IFieldSymbol sourceField, IParameterSymbol targetParameter):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2077,
					diagnosticLocation,
					targetParameter.GetDisplayName (),
					targetParameter.ContainingSymbol.GetDisplayName (),
					sourceField.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IFieldSymbol sourceField, IMethodSymbol targetMethodReturn) when
				diagnosticLocation.SourceTree?.GetRoot ().FindNode (diagnosticLocation.SourceSpan) is ReturnStatementSyntax:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2078,
					diagnosticLocation,
					targetMethodReturn.GetDisplayName (),
					sourceField.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IFieldSymbol sourceField, IFieldSymbol targetField):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2079,
					diagnosticLocation,
					targetField.GetDisplayName (),
					sourceField.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IFieldSymbol sourceFieldSymbol, IMethodSymbol targetMethodSymbol):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2080,
					diagnosticLocation,
					targetMethodSymbol.GetDisplayName (),
					sourceFieldSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			// IL2081 not generated until we have full support of MakeGenericType/MakeGenericMethod

			case (IMethodSymbol sourceMethod, IParameterSymbol targetParameter):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2082,
					diagnosticLocation,
					targetParameter.GetDisplayName (),
					targetParameter.ContainingSymbol.GetDisplayName (),
					sourceMethod.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethod, IMethodSymbol targetMethod) when
				diagnosticLocation.SourceTree?.GetRoot ().FindNode (diagnosticLocation.SourceSpan) is ReturnStatementSyntax:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2083,
					diagnosticLocation,
					targetMethod.GetDisplayName (),
					sourceMethod.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethod, IFieldSymbol targetField):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2084,
					diagnosticLocation,
					targetField.GetDisplayName (),
					sourceMethod.GetDisplayName (),
					missingMemberTypes));
				break;
			case (IMethodSymbol sourceMethod, IMethodSymbol targetMethod):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2085,
					diagnosticLocation,
					targetMethod.GetDisplayName (),
					sourceMethod.GetDisplayName (),
					missingMemberTypes));
				break;
			// IL2086 not generated until we have full support of MakeGenericType/MakeGenericMethod

			case (ITypeParameterSymbol sourceGenericParameter, IParameterSymbol targetParameter):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2087,
					diagnosticLocation,
					targetParameter.GetDisplayName (),
					targetParameter.ContainingSymbol.GetDisplayName (),
					sourceGenericParameter.GetDisplayName (),
					sourceGenericParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			case (ITypeParameterSymbol sourceGenericParameter, IMethodSymbol targetMethod) when
				diagnosticLocation.SourceTree?.GetRoot ().FindNode (diagnosticLocation.SourceSpan) is ReturnStatementSyntax:
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2088,
					diagnosticLocation,
					targetMethod.GetDisplayName (),
					sourceGenericParameter.GetDisplayName (),
					sourceGenericParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			case (ITypeParameterSymbol sourceGenericParameter, IFieldSymbol targetField):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2089,
					diagnosticLocation,
					targetField.GetDisplayName (),
					sourceGenericParameter.GetDisplayName (),
					sourceGenericParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			// IL2090 currently not generated
			case (ITypeParameterSymbol sourceGenericParameter, ITypeParameterSymbol targetGenericParameter):
				context.ReportDiagnostic (Diagnostic.Create (
					s_IL2091,
					diagnosticLocation,
					targetGenericParameter.GetDisplayName (),
					targetGenericParameter.ContainingSymbol.GetDisplayName (),
					sourceGenericParameter.GetDisplayName (),
					sourceGenericParameter.ContainingSymbol.GetDisplayName (),
					missingMemberTypes));
				break;
			}
		}
	}
}
