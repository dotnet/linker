// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		public const string IL2026 = nameof (IL2026);
		public const string IL2046 = nameof (IL2046);
		public const string IL2107 = nameof (IL2107);
		public const string IL2108 = nameof (IL2108);
		public const string IL2109 = nameof (IL2109);
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		public const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		static readonly DiagnosticDescriptor s_requiresUnreferencedCodeRule = new DiagnosticDescriptor (
			IL2026,
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		static readonly DiagnosticDescriptor s_baseRequiresMismatch = new DiagnosticDescriptor (
			IL2046,
			new LocalizableResourceString (nameof (Resources.BaseRequiresMismatchTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.BaseRequiresMismatchMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		static readonly DiagnosticDescriptor s_derivedRequiresMismatch = new DiagnosticDescriptor (
			IL2107,
			new LocalizableResourceString (nameof (Resources.DerivedRequiresMismatchTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.DerivedRequiresMismatchMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		static readonly DiagnosticDescriptor s_interfaceRequiresMismatch = new DiagnosticDescriptor (
			IL2108,
			new LocalizableResourceString (nameof (Resources.InterfaceRequiresMismatchTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.InterfaceRequiresMismatchMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		static readonly DiagnosticDescriptor s_implementationRequiresMismatch = new DiagnosticDescriptor (
			IL2109,
			new LocalizableResourceString (nameof (Resources.ImplementationRequiresMismatchTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.ImplementationRequiresMismatchMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresUnreferencedCodeRule, s_baseRequiresMismatch, s_derivedRequiresMismatch, s_interfaceRequiresMismatch, s_implementationRequiresMismatch);

		private protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		private protected override DiagnosticDescriptor BaseRequiresMismatch => s_baseRequiresMismatch;

		private protected override DiagnosticDescriptor DerivedRequiresMismatch => s_derivedRequiresMismatch;

		private protected override DiagnosticDescriptor InterfaceRequiresMismatch => s_interfaceRequiresMismatch;

		private protected override DiagnosticDescriptor ImplementationRequiresMismatch => s_implementationRequiresMismatch;

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation)
		{
			var isTrimAnalyzerEnabled = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);
			if (!string.Equals (isTrimAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) =>
			attribute.ConstructorArguments.Length >= 1 && attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			if (!string.IsNullOrEmpty (message))
				message = $" {message}{(message.TrimEnd ().EndsWith (".") ? "" : ".")}";

			return message;
		}
	}
}
