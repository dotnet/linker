// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using ILLink.CodeFixProvider;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace ILLink.CodeFix
{
	[ExportCodeFixProvider (LanguageNames.CSharp, Name = nameof (UnconditionalSuppressMessageCodeFixProvider)), Shared]
	public class UnconditionalSuppressMessageCodeFixProvider : BaseAttributeCodeFixProvider
	{
		const string UnconditionalSuppressMessageAttribute = nameof (UnconditionalSuppressMessageAttribute);
		public const string FullyQualifiedUnconditionalSuppressMessageAttribute = "System.Diagnostics.CodeAnalysis." + UnconditionalSuppressMessageAttribute;

		public sealed override ImmutableArray<string> FixableDiagnosticIds
			=> (new DiagnosticId[] {
				DiagnosticId.RequiresUnreferencedCode,
				DiagnosticId.AvoidAssemblyLocationInSingleFile,
				DiagnosticId.AvoidAssemblyGetFilesInSingleFile,
				DiagnosticId.RequiresAssemblyFiles,
				DiagnosticId.RequiresDynamicCode }).Select (d => d.AsString ()).ToImmutableArray ();

		private protected override LocalizableString CodeFixTitle => new LocalizableResourceString (nameof (Resources.UconditionalSuppressMessageCodeFixTitle), Resources.ResourceManager, typeof (Resources));

		private protected override string FullyQualifiedAttributeName => FullyQualifiedUnconditionalSuppressMessageAttribute;

		private protected override AttributeableParentTargets AttributableParentTargets => AttributeableParentTargets.All;

		public sealed override Task RegisterCodeFixesAsync (CodeFixContext context) => BaseRegisterCodeFixesAsync (context);
	}
}
