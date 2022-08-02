// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
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
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter));
			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private static readonly string[] AttributeOnReturn = { 
			DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings.AsString (), 
			DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString (), 
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType.AsString()
		};

		protected static SyntaxNode[] GetAttributeArguments (ISymbol? targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			if (targetSymbol == null)
				return Array.Empty<SyntaxNode> ();
			object id = Enum.Parse (typeof (DiagnosticId), diagnostic.Id.Substring (2));
			switch (id) {
			case DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			case DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			case DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			case DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.MemberAccessExpression (syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), "PublicMethods")) };
			case DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField:
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.MemberAccessExpression (syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), "PublicMethods")) };
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
			bool ReturnAttribute = false;
			var document = context.Document;
			if (await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false) is not { } root)
				return;
			var diagnostic = context.Diagnostics[0];

			SyntaxNode attributableNode;
			if (diagnostic.AdditionalLocations.Count != 0) {
				attributableNode = root.FindNode (diagnostic.AdditionalLocations[0].SourceSpan, getInnermostNodeForTie: true);
			} else {
				return;
			}

			if (attributableNode is null) return;

			string? stringArgs = diagnostic.Properties["attributeArgument"];

			if (stringArgs == null) {
				return;
			}

			var syntaxGenerator = SyntaxGenerator.GetGenerator (document);

			var attributeArguments = new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.MemberAccessExpression (syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes"), stringArgs)) };

			if (await document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false) is not { } model)
				return;

			SyntaxNode originalAttributeNode = root.FindNode (diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);

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
					case TypeOfExpressionSyntax:
						break;
					default:
						return;
					}
				}
			

			var codeFixTitle = CodeFixTitle.ToString ();

			if (AttributeOnReturn.Contains (diagnostic.Id)) {
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
			bool addAsReturnAttribute,
			CancellationToken cancellationToken)
		{
			var editor = await DocumentEditor.CreateAsync (document, cancellationToken).ConfigureAwait (false);
			var generator = editor.Generator;
			var attribute = generator.Attribute (
				generator.TypeExpression (attributeSymbol), attributeArguments)
				.WithAdditionalAnnotations (Simplifier.Annotation, Simplifier.AddImportsAnnotation);

			if (addAsReturnAttribute) {
				// don't use AddReturnAttribute because it's the same as AddAttribute (bug)
				editor.ReplaceNode (targetNode, (d, g) => g.AddReturnAttributes (d, new[] { attribute }));
			} else {
				editor.AddAttribute (targetNode, attribute);
			}
			return editor.GetChangedDocument ();
		}
	}
}
