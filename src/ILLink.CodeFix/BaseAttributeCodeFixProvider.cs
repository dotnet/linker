// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	public abstract class BaseAttributeCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		private protected abstract LocalizableString CodeFixTitle { get; }

		private protected abstract string FullyQualifiedAttributeName { get; }

		private protected abstract AttributeableParentTargets AttributableParentTargets { get; }

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		protected async Task BaseRegisterCodeFixesAsync (CodeFixContext context)
		{
			var document = context.Document;
			var diagnostic = context.Diagnostics.First ();
			var codeFixTitle = CodeFixTitle.ToString ();

			if (await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false) is not { } root)
				return;

			SyntaxNode targetNode = root.FindNode (diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			if (FindAttributableParent (targetNode, AttributableParentTargets) is not SyntaxNode attributableNode)
				return;

			context.RegisterCodeFix (CodeAction.Create (
				title: codeFixTitle,
				createChangedDocument: ct => AddAttributeAsync (
					document, diagnostic, targetNode, attributableNode, FullyQualifiedAttributeName, ct),
				equivalenceKey: codeFixTitle), diagnostic);
		}

		private static async Task<Document> AddAttributeAsync (
			Document document,
			Diagnostic diagnostic,
			SyntaxNode targetNode,
			SyntaxNode attributableNode,
			string fullyQualifiedAttributeName,
			CancellationToken cancellationToken)
		{
			if (await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false) is not { } model)
				return document;
			if (model.GetSymbolInfo (targetNode, cancellationToken).Symbol is not { } targetSymbol)
				return document;
			if (model.Compilation.GetBestTypeByMetadataName (fullyQualifiedAttributeName) is not { } attributeSymbol)
				return document;

			// N.B. May be null for FieldDeclaration, since field declarations can declare multiple variables
			var attributableSymbol = model.GetDeclaredSymbol (attributableNode, cancellationToken);

			var attributeArguments = fullyQualifiedAttributeName == UnconditionalSuppressMessageCodeFixProvider.FullyQualifiedUnconditionalSuppressMessageAttribute ?
				GetAttributeArgumentsForUnconditionalSuppressMessageAttribute (SyntaxGenerator.GetGenerator (document), diagnostic) :
				GetAttributeArgumentsForRequiresAttribute (attributableSymbol, targetSymbol, SyntaxGenerator.GetGenerator (document));

			var editor = await DocumentEditor.CreateAsync (document, cancellationToken).ConfigureAwait (false);
			var generator = editor.Generator;
			var attribute = generator.Attribute (
				generator.TypeExpression (attributeSymbol), attributeArguments)
				.WithAdditionalAnnotations (Simplifier.Annotation, Simplifier.AddImportsAnnotation);

			editor.AddAttribute (attributableNode, attribute);
			return editor.GetChangedDocument ();
		}

		[Flags]
		protected enum AttributeableParentTargets
		{
			MethodOrConstructor = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			All = MethodOrConstructor | Property | Field | Event
		}

		private static CSharpSyntaxNode? FindAttributableParent (SyntaxNode node, AttributeableParentTargets targets)
		{
			SyntaxNode? parentNode = node.Parent;
			while (parentNode is not null) {
				switch (parentNode) {
				case LambdaExpressionSyntax:
					return null;

				case LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.MethodOrConstructor):
				case PropertyDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Property):
				case FieldDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Field):
				case EventDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Event):
					return (CSharpSyntaxNode) parentNode;

				default:
					parentNode = parentNode.Parent;
					break;
				}
			}

			return null;
		}

		private static SyntaxNode[] GetAttributeArgumentsForRequiresAttribute (ISymbol? attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator)
		{
			var symbolDisplayName = targetSymbol.GetDisplayName ();
			if (string.IsNullOrEmpty (symbolDisplayName) || HasPublicAccessibility (attributableSymbol))
				return Array.Empty<SyntaxNode> ();

			return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.LiteralExpression ($"Calls {symbolDisplayName}")) };
		}

		private static SyntaxNode[] GetAttributeArgumentsForUnconditionalSuppressMessageAttribute (SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			// Category of the attribute
			var ruleCategory = syntaxGenerator.AttributeArgument (
				syntaxGenerator.LiteralExpression (diagnostic.Descriptor.Category));

			// Identifier of the analysis rule the attribute applies to
			var ruleTitle = diagnostic.Descriptor.Title.ToString (CultureInfo.CurrentUICulture);
			var ruleId = syntaxGenerator.AttributeArgument (
				syntaxGenerator.LiteralExpression (
					string.IsNullOrWhiteSpace (ruleTitle) ? diagnostic.Id : $"{diagnostic.Id}:{ruleTitle}"));

			// The user should provide a justification for the suppression
			var suppressionJustification = syntaxGenerator.AttributeArgument ("Justification",
				syntaxGenerator.LiteralExpression ("<Pending>"));

			// [UnconditionalSuppressWarning (category, id, Justification = "<Pending>")]
			return new[] { ruleCategory, ruleId, suppressionJustification };
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
