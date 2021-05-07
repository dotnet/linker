// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresUnreferencedCodeCodeFixProvider)), Shared]
	public class RequiresUnreferencedCodeCodeFixProvider : BaseAttributeCodeFixProvider
	{
		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresUnreferencedCodeAnalyzer.IL2026);

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			await BaseRegisterCodeFixesAsync (
				context: context,
				targets: AttributeableParentTargets.MethodOrConstructor,
				fullyQualifiedAttributeName: RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute,
				title: new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeCodeFixTittle), Resources.ResourceManager, typeof (Resources)));
		}

		internal override SyntaxNode[] GetAttributeArguments (SemanticModel semanticModel, SyntaxNode targetNode, CSharpSyntaxNode containingDecl, SyntaxGenerator generator, Diagnostic diagnostic)
		{
			var containingSymbol = semanticModel.GetDeclaredSymbol (containingDecl);
			var name = semanticModel.GetSymbolInfo (targetNode).Symbol?.Name;
			if (string.IsNullOrEmpty (name) || HasPublicAccessibility (containingSymbol!)) {
				return Array.Empty<SyntaxNode> ();
			} else {
				return new[] { generator.AttributeArgument ("Message", generator.LiteralExpression ($"Calls {name}")) };
			}
		}
	}
}
