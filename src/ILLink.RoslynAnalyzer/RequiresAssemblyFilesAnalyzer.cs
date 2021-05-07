// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : RequiresAnalyzerBase
	{
		public const string IL3000 = nameof (IL3000);
		public const string IL3001 = nameof (IL3001);
		public const string IL3002 = nameof (IL3002);

		private const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		public const string RequiresAssemblyFilesAttributeFullyQualifiedName = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

		static readonly DiagnosticDescriptor s_locationRule = new DiagnosticDescriptor (
			IL3000,
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyLocationInSingleFileTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyLocationInSingleFileMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3000");

		static readonly DiagnosticDescriptor s_getFilesRule = new DiagnosticDescriptor (
			IL3001,
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyGetFilesInSingleFileTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.AvoidAssemblyGetFilesInSingleFileMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3001");

		static readonly DiagnosticDescriptor s_requiresAssemblyFilesRule = new DiagnosticDescriptor (
			IL3002,
			new LocalizableResourceString (nameof (Resources.RequiresAssemblyFilesTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresAssemblyFilesMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true,
			helpLinkUri: "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3002");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_locationRule, s_getFilesRule, s_requiresAssemblyFilesRule);

		protected override string RequiresAttributeName => RequiresAssemblyFilesAttribute;

		protected override string RequiresAttributeFullyQualifiedName => RequiresAssemblyFilesAttributeFullyQualifiedName;

		protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Property | DiagnosticTargets.Event;

		protected override DiagnosticDescriptor RequiresDiagnosticRule => s_requiresAssemblyFilesRule;

		protected override bool VerifyMSBuildOptions (CompilationStartAnalysisContext context, Compilation compilation)
		{
			var isSingleFileAnalyzerEnabled = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer, compilation);
			if (!string.Equals (isSingleFileAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return true;
			var includesAllContent = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
			if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return true;
			return false;
		}

		protected override ImmutableArray<ISymbol> GetDangerousPatterns (Compilation compilation)
		{
			var dangerousPatternsBuilder = ImmutableArray.CreateBuilder<ISymbol> ();

			var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
			if (assemblyType != null) {
				// properties
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

				// methods
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
				dangerousPatternsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
			}

			var assemblyNameType = compilation.GetTypeByMetadataName ("System.Reflection.AssemblyName");
			if (assemblyNameType != null) {
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
			}
			return dangerousPatternsBuilder.ToImmutable ();
		}

		protected override bool ReportDangerousPatternDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> dangerousPatterns, ISymbol member)
		{
			if (member is IMethodSymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (s_getFilesRule, operationContext.Operation.Syntax.GetLocation (), member));
				return true;
			} else if (member is IPropertySymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (s_locationRule, operationContext.Operation.Syntax.GetLocation (), member));
				return true;
			}
			return false;
		}

		protected override bool TryGetRequiresAttribute (ISymbol member, out AttributeData? requiresAttribute)
		{
			requiresAttribute = null;
			foreach (var _attribute in member.GetAttributes ()) {
				if (_attribute.AttributeClass is var attrClass && attrClass != null &&
					attrClass.HasName (RequiresAssemblyFilesAttributeFullyQualifiedName) &&
					_attribute.ConstructorArguments.Length == 0) {
					requiresAttribute = _attribute;
					return true;
				}
			}
			return false;
		}

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = requiresAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Message").Value.Value?.ToString ();
			return string.IsNullOrEmpty (message) ? "" : $" {message}.";
		}
	}
}
