﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresAssemblyFilesCodeFixProvider)), Shared]
	public class RequiresAssemblyFilesCodeFixProvider : BaseAttributeCodeFixProvider
	{
		private const string s_title = "Add RequiresAssemblyFiles attribute to parent method";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresAssemblyFilesAnalyzer.IL3000, RequiresAssemblyFilesAnalyzer.IL3001, RequiresAssemblyFilesAnalyzer.IL3002);

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			await BaseRegisterCodeFixesAsync (context, AttributeableParentTargets.Method | AttributeableParentTargets.Property | AttributeableParentTargets.Event, RequiresAssemblyFilesAnalyzer.FullyQualifiedRequiresAssemblyFilesAttribute, s_title);
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
