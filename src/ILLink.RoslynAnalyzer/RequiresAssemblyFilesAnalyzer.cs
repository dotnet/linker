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
	[AddSupportedDiagnostic ("IL3000", "AvoidAssemblyLocationInSingleFile",
		Category = DiagnosticCategory.SingleFile,
		HelpLinkURI = "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3000")]
	[AddSupportedDiagnostic ("IL3001", "AvoidAssemblyGetFilesInSingleFile",
		Category = DiagnosticCategory.SingleFile,
		HelpLinkURI = "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3001")]
	[AddSupportedDiagnostic ("IL3002", "RequiresAssemblyFiles",
		Category = DiagnosticCategory.SingleFile,
		HelpLinkURI = "https://docs.microsoft.com/dotnet/fundamentals/code-analysis/quality-rules/il3002")]
	[AddSupportedDiagnostic ("IL3003", "RequiresAttributeMismatch")]
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : RequiresAnalyzerBase
	{
		private const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		public const string RequiresAssemblyFilesAttributeFullyQualifiedName = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
			ImmutableArray.Create (Diagnostics.GetSupportedDiagnosticsOnType (typeof (RequiresAssemblyFilesAnalyzer)));

		private protected override string RequiresAttributeName => RequiresAssemblyFilesAttribute;

		private protected override string RequiresAttributeFullyQualifiedName => RequiresAssemblyFilesAttributeFullyQualifiedName;

		private protected override DiagnosticTargets AnalyzerDiagnosticTargets => DiagnosticTargets.MethodOrConstructor | DiagnosticTargets.Property | DiagnosticTargets.Event;

		private protected override DiagnosticDescriptor RequiresDiagnosticRule => Diagnostics.GetDiagnostic ("IL3002");

		private protected override DiagnosticDescriptor RequiresAttributeMismatch => Diagnostics.GetDiagnostic ("IL3003");

		protected override bool IsAnalyzerEnabled (AnalyzerOptions options, Compilation compilation)
		{
			var isSingleFileAnalyzerEnabled = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer, compilation);
			if (!string.Equals (isSingleFileAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			var includesAllContent = options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
			if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
				return false;
			return true;
		}

		protected override ImmutableArray<ISymbol> GetSpecialIncompatibleMembers (Compilation compilation)
		{
			var dangerousPatternsBuilder = ImmutableArray.CreateBuilder<ISymbol> ();

			var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
			if (assemblyType != null) {
				// Properties
				ImmutableArrayOperations.AddIfNotNull (dangerousPatternsBuilder, ImmutableArrayOperations.TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

				// Methods
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

		protected override bool ReportSpecialIncompatibleMembersDiagnostic (OperationAnalysisContext operationContext, ImmutableArray<ISymbol> dangerousPatterns, ISymbol member)
		{
			if (member is IPropertySymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (Diagnostics.GetDiagnostic ("IL3000"),
					operationContext.Operation.Syntax.GetLocation (), member.GetDisplayName ()));

				return true;
			} else if (member is IMethodSymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
				operationContext.ReportDiagnostic (Diagnostic.Create (Diagnostics.GetDiagnostic ("IL3001"),
					operationContext.Operation.Syntax.GetLocation (), member.GetDisplayName ()));

				return true;
			}

			return false;
		}

		protected override bool VerifyAttributeArguments (AttributeData attribute) => attribute.ConstructorArguments.Length == 0;

		protected override string GetMessageFromAttribute (AttributeData? requiresAttribute)
		{
			var message = requiresAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Message").Value.Value?.ToString ();
			if (!string.IsNullOrEmpty (message))
				message = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";

			return message!;
		}
	}
}
