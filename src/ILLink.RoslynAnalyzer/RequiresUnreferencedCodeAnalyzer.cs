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
	public class RequiresUnreferencedCodeAnalyzer : RequiresAnalyzerBase
	{
		public const string IL2026 = nameof (IL2026);
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

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_requiresUnreferencedCodeRule);

		protected override string RequiresAttributeName => RequiresUnreferencedCodeAttribute;

		protected override string RequiresAttributeFullyQualifiedName => FullyQualifiedRequiresUnreferencedCodeAttribute;

		protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor;

		protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresUnreferencedCodeRule;

		protected override ImmutableArray<ISymbol> GetDangerousPatterns (Compilation compilation)
		{
			return new ImmutableArray<ISymbol> ();
		}

		protected override bool ReportDangerousPatternDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> dangerousPatterns, ISymbol member)
		{
			return false;
		}

		protected override bool VerifyMSBuildOptions (CompilationStartAnalysisContext context, Compilation compilation)
		{
			var isTrimAnalyzerEnabled = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation);
			if (!string.Equals (isTrimAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}

		protected override bool TryGetRequiresAttribute (ISymbol member, out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			foreach (var _attribute in member.GetAttributes ()) {
				if (_attribute.AttributeClass is var attrClass && attrClass != null &&
					attrClass.HasName (RequiresAttributeFullyQualifiedName) && _attribute.ConstructorArguments.Length >= 1 &&
					_attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg) {
					requiresAttribute = _attribute;
					return true;
				}
			}
			return false;
		}

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = (string) requiresAttribute!.ConstructorArguments[0].Value!;
			return message != string.Empty ? " " + message + "." : message;
		}
	}
}
