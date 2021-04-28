// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresUnreferencedCodeCodeFixProvider)), Shared]
	public class RequiresUnreferencedCodeCodeFixProvider : CodeFixProvider
	{
		private const string s_title = "Add RequiresUnreferencedCode attribute to parent method";

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresUnreferencedCodeAnalyzer.DiagnosticId);

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public sealed override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var root = await context.Document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);

			var diagnostic = context.Diagnostics.First ();
			var diagnosticSpan = diagnostic.Location.SourceSpan;

			SyntaxNode targetNode = root!.FindNode (diagnosticSpan);

			CSharpSyntaxNode? declarationSyntax = CodeFixProviderOperations.FindAttributableParent (targetNode, CodeFixProviderOperations.AttributeableParentTargets.Method);

			if (declarationSyntax is not null) {
				var semanticModel = await context.Document
					.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
				var symbol = semanticModel!.Compilation.GetTypeByMetadataName (
					RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix (
					CodeAction.Create (
						title: s_title,
						createChangedDocument: c => CodeFixProviderOperations.AddRequiresAttribute (
							context.Document, root, targetNode, declarationSyntax, symbol!, false, c),
						equivalenceKey: s_title),
					diagnostic);
			}
		}
	}
}
