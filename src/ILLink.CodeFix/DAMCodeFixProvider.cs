// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	public class DAMCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		public static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> ();
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter));
			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		protected static SyntaxNode[] GetAttributeArguments (ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			object id = Enum.Parse (typeof (DiagnosticId), diagnostic.Id.Substring (2));
			switch (id) {
			case DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			case DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			default:
				return Array.Empty<SyntaxNode> ();
			}
		}

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			var document = context.Document;
			if (await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false) is not { } root)
				return;
			var diagnostic = context.Diagnostics.First ();
			SyntaxNode sourceNode = root.FindNode(diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
			SyntaxNode diagnosticNode = root.FindNode (diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
			if (await document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false) is not { } model)
				return;
			// Note: We get the target symbol from the diagnostic location. 
			// This works when the diagnostic location is a method call, because the target symbol will be the called method with annotations, but won't work in general for other kinds of diagnostics.
			// TODO: Fix targetSymbol so it's not necessarily derived from diagnosticNode --> use the location from DiagnosticContext.cs
			if (model.GetSymbolInfo (diagnosticNode).Symbol is not { } targetSymbol)
				return;
			if (model.Compilation.GetTypeByMetadataName (FullyQualifiedAttributeName) is not { } attributeSymbol)
				return;

			if (diagnosticNode is not InvocationExpressionSyntax invocationExpression)
				return;

			var arguments = invocationExpression.ArgumentList.Arguments;

			if (arguments.Count > 1)
				return;

			if (arguments.Count == 1) {
				switch (arguments[0].Expression) {
					case IdentifierNameSyntax:
						break;
					case LiteralExpressionSyntax literalSyntax:
						if (literalSyntax.Kind () is SyntaxKind.StringLiteralExpression)
							break;
						return;
					default:
						return;
				}
			}

			// N.B. May be null for FieldDeclaration, since field declarations can declare multiple variables

			var attributableNode = sourceNode; 

			if (attributableNode is null) return;

			var attributeArguments = GetAttributeArguments (targetSymbol, SyntaxGenerator.GetGenerator (document), diagnostic);
			var codeFixTitle = CodeFixTitle.ToString ();

			context.RegisterCodeFix (CodeAction.Create (
				title: codeFixTitle,
				createChangedDocument: ct => AddAttributeAsync (
					document, attributableNode, attributeArguments, attributeSymbol, ct),
				equivalenceKey: codeFixTitle), diagnostic);
		}

		private static async Task<Document> AddAttributeAsync (
			Document document,
			SyntaxNode targetNode,
			SyntaxNode[] attributeArguments,
			ITypeSymbol attributeSymbol,
			CancellationToken cancellationToken)
		{
			var editor = await DocumentEditor.CreateAsync (document, cancellationToken).ConfigureAwait (false);
			var generator = editor.Generator;
			var attribute = generator.Attribute (
				generator.TypeExpression (attributeSymbol), attributeArguments)
				.WithAdditionalAnnotations (Simplifier.Annotation, Simplifier.AddImportsAnnotation);

			editor.AddAttribute (targetNode, attribute);
			return editor.GetChangedDocument ();
		}
	}
}
