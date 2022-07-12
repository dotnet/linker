// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Immutable;
using ILLink.Shared;
using ILLink.RoslynAnalyzer;
using ILLink.CodeFixProvider;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	// [ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (DAMCodeFixProvider)), Shared]

	public class DAMCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DynamicallyAccessedMembersAnalyzer.GetSupportedDiagnostics();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);
		
		static ImmutableHashSet<string> AttriubuteOnReturn () {
			var diagDescriptorsArrayBuilder = ImmutableHashSet.CreateBuilder<string> ();
			diagDescriptorsArrayBuilder.Add (DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings.AsString());
			diagDescriptorsArrayBuilder.Add (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString());
			return diagDescriptorsArrayBuilder.ToImmutable();
		}

		protected static SyntaxNode[] GetAttributeArguments (ISymbol attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			var symbolDisplayName = targetSymbol.GetDisplayName ();
			if (string.IsNullOrEmpty (symbolDisplayName) || HasPublicAccessibility (attributableSymbol))
				return Array.Empty<SyntaxNode> ();

			if (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection.AsString()) {
				return new[] { syntaxGenerator.AttributeArgument ( syntaxGenerator.BitwiseOrExpression(syntaxGenerator.DottedName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields"), syntaxGenerator.DottedName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields"))) };
			}
			else if  (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection.AsString()) {
				return new[] { syntaxGenerator.AttributeArgument ( syntaxGenerator.BitwiseOrExpression(syntaxGenerator.DottedName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods"), syntaxGenerator.DottedName("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods"))) };
			}
			else if (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter.AsString()) {
				return new[] { syntaxGenerator.AttributeArgument ( syntaxGenerator.TypedConstantExpression(targetSymbol.GetAttributes().First(attr => attr.AttributeClass?.ToDisplayString() == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0]) )};
			}
			else {
				return new[] { syntaxGenerator.AttributeArgument ( syntaxGenerator.LiteralExpression("")) };
			}
		}

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		protected async Task BaseRegisterCodeFixesAsync (CodeFixContext context)
		{

			bool ReturnAttribute = false;
			var document = context.Document;
			var root = await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);
			var diagnostic = context.Diagnostics.First ();
			var props = diagnostic.Properties;
			var model = await document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
			SyntaxNode diagnosticNode = root!.FindNode (diagnostic.Location.SourceSpan);
			var attributableSymbol = (diagnosticNode is InvocationExpressionSyntax invocationExpression 
					&& invocationExpression.Expression is MemberAccessExpressionSyntax simpleMember 
					&& simpleMember.Expression is IdentifierNameSyntax name) ? model.GetSymbolInfo(name).Symbol : null;

			if (attributableSymbol is null) return;

			var attributableNodeList = attributableSymbol.DeclaringSyntaxReferences;

			var attributableNode = (attributableNodeList.Length == 1) ? attributableNodeList[0].GetSyntax() : null;

			if (attributableNode is null) return;
			var targetSymbol = model!.GetSymbolInfo (diagnosticNode).Symbol!;
			var attributeSymbol = model!.Compilation.GetTypeByMetadataName (FullyQualifiedAttributeName)!;
			var attributeArguments = GetAttributeArguments (attributableSymbol, targetSymbol, SyntaxGenerator.GetGenerator (document), diagnostic);
			var codeFixTitle = CodeFixTitle.ToString ();
			var returnAttributes = AttriubuteOnReturn ();
			if (returnAttributes.Contains(attributeSymbol.ToString())) {
				ReturnAttribute = true;
			}

			context.RegisterCodeFix (CodeAction.Create (
				title: codeFixTitle,
				createChangedDocument: ct => AddAttributeAsync (
					document, attributableNode, attributeArguments, attributeSymbol, ReturnAttribute, ct),
				equivalenceKey: codeFixTitle), diagnostic);
		}

		private static async Task<Document> AddAttributeAsync (
			Document document,
			SyntaxNode targetNode,
			SyntaxNode[] attributeArguments,
			ITypeSymbol attributeSymbol,
			bool AddAsReturnAttribute,
			CancellationToken cancellationToken)
		{
			var editor = await DocumentEditor.CreateAsync (document, cancellationToken).ConfigureAwait (false);
			var generator = editor.Generator;
			var attribute = generator.Attribute (
				generator.TypeExpression (attributeSymbol), attributeArguments)
				.WithAdditionalAnnotations (Simplifier.Annotation, Simplifier.AddImportsAnnotation);

			if (AddAsReturnAttribute)
			{
				editor.AddReturnAttribute(targetNode, attribute);
			} 
			else
			{
				editor.AddAttribute (targetNode, attribute);
			}
			return editor.GetChangedDocument ();
		}

		protected static bool HasPublicAccessibility (ISymbol? m)
		{
			if (m is not { DeclaredAccessibility: Accessibility.Public or Accessibility.Protected }) {
				return false;
			}
			for (var t = m.ContainingType; t is not null; t = t.ContainingType) {
				if (t.DeclaredAccessibility != Accessibility.Public) {
					return false;
				}
			}
			return true;
		}
	}
}
