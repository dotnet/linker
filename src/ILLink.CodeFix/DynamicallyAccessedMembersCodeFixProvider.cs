// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.RoslynAnalyzer;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Editing;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (DynamicallyAccessedMemberCodeFixProvider)), Shared]
	public class DynamicallyAccessedMemberCodeFixProvider : BaseAttributeCodeFixProvider
	{
		public static ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersFieldAccessedViaReflection),
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection));

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.DynamicallyAccessedMembersCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => DynamicallyAccessedMembersAnalyzer.FullyQualifiedDynamicallyAccessedMembersAttribute;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

		protected override SyntaxNode[] GetAttributeArguments (ISymbol attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
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
			else {
				return new[] { syntaxGenerator.AttributeArgument ( syntaxGenerator.LiteralExpression("")) };
			}
		}
	}
}
