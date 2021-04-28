// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	class CodeFixProviderOperations
	{

		internal static async Task<Document> AddRequiresAttribute (
			Document document,
			SyntaxNode root,
			SyntaxNode targetNode,
			CSharpSyntaxNode containingDecl,
			ITypeSymbol Symbol,
			bool isNamedArgument,
			CancellationToken cancellationToken)
		{
			var editor = new SyntaxEditor (root, document.Project.Solution.Workspace);
			var generator = editor.Generator;

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (semanticModel is null) {
				return document;
			}
			var containingSymbol = (IMethodSymbol?) semanticModel.GetDeclaredSymbol (containingDecl);
			var name = semanticModel.GetSymbolInfo (targetNode).Symbol?.Name;
			SyntaxNode[] attrArgs;
			if (string.IsNullOrEmpty (name) || HasPublicAccessibility (containingSymbol)) {
				attrArgs = Array.Empty<SyntaxNode> ();
			} else {
				attrArgs = new[] { isNamedArgument ? generator.AttributeArgument ("Message", generator.LiteralExpression ($"Calls {name}")) : generator.AttributeArgument (generator.LiteralExpression ($"Calls {name}")) };
			}
			var newAttribute = generator
				.Attribute (generator.TypeExpression (Symbol), attrArgs)
				.WithAdditionalAnnotations (
					Simplifier.Annotation,
					Simplifier.AddImportsAnnotation);

			editor.AddAttribute (containingDecl, newAttribute);

			return document.WithSyntaxRoot (editor.GetChangedRoot ());
		}

		private static bool HasPublicAccessibility (IMethodSymbol? m)
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

		[Flags]
		public enum AttributeableParentTargets
		{
			Method = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			All = Method | Property | Field | Event
		}

		internal static CSharpSyntaxNode? FindAttributableParent (SyntaxNode node, AttributeableParentTargets targets)
		{
			SyntaxNode? parentNode = node.Parent;
			while (parentNode is not null) {
				switch (parentNode) {
				case LambdaExpressionSyntax:
					return null;
				case LocalFunctionStatementSyntax or BaseMethodDeclarationSyntax when targets.HasFlag (AttributeableParentTargets.Method):
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
	}
}
