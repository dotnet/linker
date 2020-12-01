// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	/// <summary>
	/// IL3000, IL3001: Do not use Assembly file path in single-file publish
	/// </summary>
	[DiagnosticAnalyzer (LanguageNames.CSharp, LanguageNames.VisualBasic)]
	public sealed class AvoidAssemblyLocationInSingleFile : DiagnosticAnalyzer
	{
		public const string IL3000 = nameof (IL3000);
		public const string IL3001 = nameof (IL3001);

		private static readonly DiagnosticDescriptor LocationRule = new DiagnosticDescriptor (
			IL3000,
			new LocalizableResourceString (nameof (ILLinkRoslynAnalyzerResources.AvoidAssemblyLocationInSingleFileTitle),
				ILLinkRoslynAnalyzerResources.ResourceManager, typeof (ILLinkRoslynAnalyzerResources)),
			new LocalizableResourceString (nameof (ILLinkRoslynAnalyzerResources.AvoidAssemblyLocationInSingleFileMessage),
				ILLinkRoslynAnalyzerResources.ResourceManager, typeof (ILLinkRoslynAnalyzerResources)),
			DiagnosticCategory.Publish,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		private static readonly DiagnosticDescriptor GetFilesRule = new DiagnosticDescriptor (
			IL3001,
			new LocalizableResourceString (nameof (ILLinkRoslynAnalyzerResources.AvoidAssemblyGetFilesInSingleFileTitle),
				ILLinkRoslynAnalyzerResources.ResourceManager, typeof (ILLinkRoslynAnalyzerResources)),
			new LocalizableResourceString (nameof (ILLinkRoslynAnalyzerResources.AvoidAssemblyGetFilesInSingleFileMessage),
				ILLinkRoslynAnalyzerResources.ResourceManager, typeof (ILLinkRoslynAnalyzerResources)),
			DiagnosticCategory.Publish,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (LocationRule, GetFilesRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isSingleFilePublish = context.Options.GetMSBuildPropertyValue (
					MSBuildPropertyOptionNames.PublishSingleFile, compilation, context.CancellationToken);
				if (!string.Equals (isSingleFilePublish?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}
				var includesAllContent = context.Options.GetMSBuildPropertyValue (
					MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation, context.CancellationToken);
				if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}

				var propertiesBuilder = ImmutableArray.CreateBuilder<IPropertySymbol> ();
				var methodsBuilder = ImmutableArray.CreateBuilder<IMethodSymbol> ();

				if (compilation.TryGetOrCreateTypeByMetadataName (WellKnownTypeNames.SystemReflectionAssembly, out var assemblyType)) {
					// properties
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

					// methods
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
				}

				if (compilation.TryGetOrCreateTypeByMetadataName (WellKnownTypeNames.SystemReflectionAssemblyName, out var assemblyNameType)) {
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
				}

				var properties = propertiesBuilder.ToImmutable ();
				var methods = methodsBuilder.ToImmutable ();

				context.RegisterOperationAction (operationContext => {
					var access = (IPropertyReferenceOperation) operationContext.Operation;
					var property = access.Property;
					if (!Contains (properties, property, SymbolEqualityComparer.Default)) {
						return;
					}

					operationContext.ReportDiagnostic (Diagnostic.Create (LocationRule, access.Syntax.GetLocation ()));
				}, OperationKind.PropertyReference);

				context.RegisterOperationAction (operationContext => {
					var invocation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = invocation.TargetMethod;
					if (!Contains (methods, targetMethod, SymbolEqualityComparer.Default)) {
						return;
					}

					operationContext.ReportDiagnostic (Diagnostic.Create (GetFilesRule, invocation.Syntax.GetLocation ()));
				}, OperationKind.Invocation);

				return;

				static bool Contains<T, TComp> (ImmutableArray<T> list, T elem, TComp comparer)
					where TComp : IEqualityComparer<T>
				{
					foreach (var e in list) {
						if (comparer.Equals (e, elem)) {
							return true;
						}
					}
					return false;
				}

				static TSymbol? TryGetSingleSymbol<TSymbol> (ImmutableArray<ISymbol> members) where TSymbol : class, ISymbol
				{
					TSymbol? candidate = null;
					foreach (var m in members) {
						if (m is TSymbol tsym) {
							if (candidate is null) {
								candidate = tsym;
							} else {
								return null;
							}
						}
					}
					return candidate;
				}

				static void AddIfNotNull<TSymbol> (ImmutableArray<TSymbol>.Builder properties, TSymbol? p) where TSymbol : class, ISymbol
				{
					if (p != null) {
						properties.Add (p);
					}
				}
			});
		}
	}
}
