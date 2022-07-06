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
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (DynamicallyAccessedMemberCodeFixProvider)), Shared]

	public abstract class DAMCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DynamicallyAccessedMembersAnalyzer.GetSupportedDiagnostics();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private protected static AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

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
			var document = context.Document;
			var root = await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);
			var diagnostic = context.Diagnostics.First ();
			SyntaxNode targetNode = root!.FindNode (diagnostic.Location.SourceSpan);
			if (FindAttributableParent (targetNode, AttributableParentTargets) is not SyntaxNode attributableNode)
				return;


			var model = await document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
			// var nodeType = model.GetOperation ( targetNode ); 
			// tryget value for each key, read key to find the parameter name etc 
			var targetSymbol = model!.GetSymbolInfo (targetNode).Symbol!;

			var attributableSymbol = model!.GetDeclaredSymbol (attributableNode)!;
			var attributeSymbol = model!.Compilation.GetTypeByMetadataName (FullyQualifiedAttributeName)!;
			var attributeArguments = GetAttributeArguments (attributableSymbol, targetSymbol, SyntaxGenerator.GetGenerator (document), diagnostic);
			var codeFixTitle = CodeFixTitle.ToString ();
			// check set if attribute is on return or not

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

		[Flags]
		protected enum AttributeableParentTargets
		{
			MethodOrConstructor = 0x0001,
			Property = 0x0002,
			Field = 0x0004,
			Event = 0x0008,
			Parameter = 0x0016,
			All = MethodOrConstructor | Property | Field | Event | Parameter
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
				case ParameterSyntax when targets.HasFlag (AttributeableParentTargets.Parameter):
					return (CSharpSyntaxNode) parentNode;

				default:
					parentNode = parentNode.Parent;
					break;
				}
			}

			return null;
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
