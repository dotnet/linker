﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (RequiresAssemblyFilesCodeFixProvider)), Shared]
	public class RequiresAssemblyFilesCodeFixProvider : BaseAttributeCodeFixProvider
	{
		public ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.AvoidAssemblyLocationInSingleFile),
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.AvoidAssemblyGetFilesInSingleFile),
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.RequiresAssemblyFiles));

		public sealed override ImmutableArray<string> FixableDiagnosticIds => SupportedDiagnostics.Select (dd => dd.Id).ToImmutableArray ();

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.RequiresAssemblyFilesCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => RequiresAssemblyFilesAnalyzer.RequiresAssemblyFilesAttributeFullyQualifiedName;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.MethodOrConstructor | AttributeableParentTargets.Property | AttributeableParentTargets.Event;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);

		protected override SyntaxNode[] GetAttributeArguments (ISymbol attributableSymbol, ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, Diagnostic diagnostic)
		{
			var symbolDisplayName = targetSymbol.GetDisplayName ();
			if (string.IsNullOrEmpty (symbolDisplayName) || HasPublicAccessibility (attributableSymbol))
				return Array.Empty<SyntaxNode> ();

			return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.LiteralExpression ($"Calls {symbolDisplayName}")) };
		}
	}
}
