// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.Simplification;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (DAMCodeFixProvider)), Shared]
	public sealed class DAMCodeFixProvider : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider
	{
		private static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> ();
			//2060s
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField));

			//2070s
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter));
			//2071
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter));
			//2076
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField));

			//2080s
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter));
			//2081
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter));
			//2086
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField));

			//2090s
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides));
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides));
			//2094
			diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides));

			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private static LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private static string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private static readonly string[] AttributeOnReturn = {
			DiagnosticId.DynamicallyAccessedMembersOnMethodReturnValueCanOnlyApplyToTypesOrStrings.AsString (),
			DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType.AsString() ,
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter.AsString (),
			DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField.AsString (),
			DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodReturnValueBetweenOverrides.AsString ()
		};

		private static readonly string[] AttributeOnGeneric = {
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType.AsString () ,
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter.AsString(),
			DiagnosticId.DynamicallyAccessedMembersMismatchOnGenericParameterBetweenOverrides.AsString()
		};

		public sealed override FixAllProvider GetFixAllProvider ()
		{
			// See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
			return WellKnownFixAllProviders.BatchFixer;
		}

		public override async Task RegisterCodeFixesAsync (CodeFixContext context)
		{
			bool ReturnAttribute = false;
			bool GenericAttribute = false;
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

			if (model.Compilation.GetTypeByMetadataName (FullyQualifiedAttributeName) is not { } attributeSymbol)
				return;

			var codeFixTitle = CodeFixTitle.ToString ();

			if (AttributeOnReturn.Contains (diagnostic.Id)) {
				ReturnAttribute = true;
			}

			if (AttributeOnGeneric.Contains (diagnostic.Id)) {
				GenericAttribute = true;
			}

			context.RegisterCodeFix (CodeAction.Create (
				title: codeFixTitle,
				createChangedDocument: ct => AddAttributeAsync (
					document, attributableNode, attributeArguments, attributeSymbol, ReturnAttribute, GenericAttribute, ct),
				equivalenceKey: codeFixTitle), diagnostic);
		}

		private static async Task<Document> AddAttributeAsync (
			Document document,
			SyntaxNode targetNode,
			SyntaxNode[] attributeArguments,
			ITypeSymbol attributeSymbol,
			bool addAsReturnAttribute,
			bool addGenericParameterAttribute,
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
			} else if (addGenericParameterAttribute) {
				// no API for adding generic Type attributes yet
				var newNode = (TypeParameterSyntax) targetNode;
				var nodeWithAttribute = newNode.AddAttributeLists ((AttributeListSyntax) attribute);
				editor.ReplaceNode (targetNode, nodeWithAttribute);
			} else {
				editor.AddAttribute (targetNode, attribute);
			}
			return editor.GetChangedDocument ();
		}
	}
}
