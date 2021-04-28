// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
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
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (UnconditionalSuppressMessageCodeFixProvider)), Shared]
	public class UnconditionalSuppressMessageCodeFixProvider : CodeFixProvider
	{
		private const string s_title = "Add UnconditionalSuppressMessage attribute to parent method";
		const string UnconditionalSuppressMessageAttribute = nameof (UnconditionalSuppressMessageAttribute);
		public const string FullyQualifiedUnconditionalSuppressMessageAttribute = "System.Diagnostics.CodeAnalysis." + UnconditionalSuppressMessageAttribute;

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> ImmutableArray.Create (RequiresUnreferencedCodeAnalyzer.DiagnosticId, RequiresAssemblyFilesAnalyzer.IL3000, RequiresAssemblyFilesAnalyzer.IL3001, RequiresAssemblyFilesAnalyzer.IL3002);

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
			CSharpSyntaxNode? declarationSyntax = CodeFixProviderOperations.FindAttributableParent (targetNode, CodeFixProviderOperations.AttributeableParentTargets.All);

			if (declarationSyntax is not null) {
				var semanticModel = await context.Document
					.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
				var symbol = semanticModel!.Compilation.GetTypeByMetadataName (FullyQualifiedUnconditionalSuppressMessageAttribute);

				// Register a code action that will invoke the fix.
				context.RegisterCodeFix (
					CodeAction.Create (
						title: s_title,
						createChangedDocument: c => AddUnconditionalSuppressMessage (
							context.Document, root, declarationSyntax, diagnostic, symbol!, c),
						equivalenceKey: s_title),
					diagnostic);
			}
		}
		private static async Task<Document> AddUnconditionalSuppressMessage (
			Document document,
			SyntaxNode root,
			CSharpSyntaxNode containingDecl,
			Diagnostic diagnostic,
			ITypeSymbol UnconditionalSuppressMessageSymbol,
			CancellationToken cancellationToken)
		{
			var editor = new SyntaxEditor (root, document.Project.Solution.Workspace);
			var generator = editor.Generator;

			var semanticModel = await document.GetSemanticModelAsync (cancellationToken).ConfigureAwait (false);
			if (semanticModel is null) {
				return document;
			}

			// UnconditionalSuppressMessage("Rule Category", "Rule Id", Justification = "<Pending>")
			var category = generator.LiteralExpression (diagnostic.Descriptor.Category);
			var categoryArgument = generator.AttributeArgument (category);

			var title = diagnostic.Descriptor.Title.ToString (CultureInfo.CurrentUICulture);
			var ruleIdText = string.IsNullOrWhiteSpace (title) ? diagnostic.Id : string.Format ("{0}:{1}", diagnostic.Id, title);
			var ruleId = generator.LiteralExpression (ruleIdText);
			var ruleIdArgument = generator.AttributeArgument (ruleId);

			var justificationExpr = generator.LiteralExpression ("<Pending>");
			var justificationArgument = generator.AttributeArgument ("Justification", justificationExpr);

			SyntaxNode[] attrArgs = new[] { categoryArgument, ruleIdArgument, justificationArgument };

			var newAttribute = generator
				.Attribute (generator.TypeExpression (UnconditionalSuppressMessageSymbol), attrArgs)
				.WithAdditionalAnnotations (
					Simplifier.Annotation,
					Simplifier.AddImportsAnnotation);

			editor.AddAttribute (containingDecl, newAttribute);

			return document.WithSyntaxRoot (editor.GetChangedRoot ());
		}
	}
}
