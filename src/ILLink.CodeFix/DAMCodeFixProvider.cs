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
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType));
			return diagDescriptorsArrayBuilder.ToImmutable ();
		} 

		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private static readonly string[] AttributeOnReturn = {DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings.AsString (), DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString ()};

		protected static SyntaxNode[] GetAttributeArguments (ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			// if (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection.AsString ()) {
			// 	return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.BitwiseOrExpression (syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicFields"), syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields"))) };
			// } else if (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection.AsString ()) {
			// 	return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.BitwiseOrExpression (syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods"), syntaxGenerator.DottedName ("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods"))) };
			// } else 
			if (diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter.AsString () || diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter.AsString () || diagnostic.Id == DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType.AsString ()) {
				return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.TypedConstantExpression (targetSymbol.GetAttributes ().First (attr => attr.AttributeClass?.ToDisplayString () == DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute).ConstructorArguments[0])) };
			} else {
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
			var root = await document.GetSyntaxRootAsync (context.CancellationToken).ConfigureAwait (false);
			var diagnostic = context.Diagnostics.First ();
			var props = diagnostic.Properties;
			var model = await document.GetSemanticModelAsync (context.CancellationToken).ConfigureAwait (false);
			SyntaxNode diagnosticNode = root!.FindNode (diagnostic.Location.SourceSpan);
			var attributableSymbol = (diagnosticNode is InvocationExpressionSyntax invocationExpression
					&& invocationExpression.Expression is MemberAccessExpressionSyntax simpleMember
					&& simpleMember.Expression is IdentifierNameSyntax name) ? model.GetSymbolInfo (name).Symbol : null;

			if (attributableSymbol is null) 
				return;

			var attributableNodeList = attributableSymbol.DeclaringSyntaxReferences;

			if (attributableNodeList.Length != 1)
				return;

			var attributableNode = attributableNodeList[0].GetSyntax ();

			if (attributableNode is null) return;
			var diagnosticSymbol = model!.GetSymbolInfo (diagnosticNode).Symbol!;
			var attributeSymbol = model!.Compilation.GetTypeByMetadataName (FullyQualifiedAttributeName)!;
			var attributeArguments = GetAttributeArguments (diagnosticSymbol, SyntaxGenerator.GetGenerator (document), diagnostic);
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
				editor.AddReturnAttribute (targetNode, attribute);
			} else {
				editor.AddAttribute (targetNode, attribute);
			}
			return editor.GetChangedDocument ();
		}
	}
}
