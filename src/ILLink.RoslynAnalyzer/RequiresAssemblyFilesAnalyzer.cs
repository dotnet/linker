// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class RequiresAssemblyFilesAnalyzer : DiagnosticAnalyzer
	{
		public const string IL3000 = nameof (IL3000);
		public const string IL3001 = nameof (IL3001);
		public const string IL3002 = nameof (IL3002);

		internal const string RequiresAssemblyFilesAttribute = nameof (RequiresAssemblyFilesAttribute);
		internal const string FullyQualifiedRequiresAssemblyFilesAttribute = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

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
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_locationRule, s_getFilesRule, s_requiresAssemblyFilesRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isSingleFileAnalyzerEnabled = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer, compilation);
				if (!string.Equals (isSingleFileAnalyzerEnabled?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				var includesAllContent = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
				if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				var propertiesBuilder = ImmutableArray.CreateBuilder<IPropertySymbol> ();
				var methodsBuilder = ImmutableArray.CreateBuilder<IMethodSymbol> ();

				var assemblyType = compilation.GetTypeByMetadataName ("System.Reflection.Assembly");
				if (assemblyType != null) {
					// properties
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyType.GetMembers ("Location")));

					// methods
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFile").OfType<IMethodSymbol> ());
					methodsBuilder.AddRange (assemblyType.GetMembers ("GetFiles").OfType<IMethodSymbol> ());
				}

				var assemblyNameType = compilation.GetTypeByMetadataName ("System.Reflection.AssemblyName");
				if (assemblyNameType != null) {
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("CodeBase")));
					AddIfNotNull (propertiesBuilder, TryGetSingleSymbol<IPropertySymbol> (assemblyNameType.GetMembers ("EscapedCodeBase")));
				}

				var properties = propertiesBuilder.ToImmutable ();
				var methods = methodsBuilder.ToImmutable ();

				context.RegisterOperationAction (operationContext => {
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = methodInvocation.TargetMethod;
					if (!operationContext.ContainingSymbol.HasAttribute (RequiresAssemblyFilesAttribute) && Contains (methods, targetMethod, SymbolEqualityComparer.Default)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_getFilesRule, methodInvocation.Syntax.GetLocation (), targetMethod));
						return;
					}
					CheckCalledMember (operationContext, targetMethod);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var objectCreation = (IObjectCreationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, objectCreation.Constructor);
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					if (!operationContext.ContainingSymbol.HasAttribute (RequiresAssemblyFilesAttribute) && Contains (properties, prop, SymbolEqualityComparer.Default)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_locationRule, propAccess.Syntax.GetLocation (), prop));
						return;
					}
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckCalledMember (operationContext, prop.GetMethod);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckCalledMember (operationContext, prop.SetMethod);

					CheckCalledMember (operationContext, prop);
				}, OperationKind.PropertyReference);

				context.RegisterOperationAction (operationContext => {
					var eventRef = (IEventReferenceOperation) operationContext.Operation;
					CheckCalledMember (operationContext, eventRef.Member);
				}, OperationKind.EventReference);

				static void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member)
				{
					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (operationContext.ContainingSymbol.HasAttribute (RequiresAssemblyFilesAttribute))
						return;

					if (member.TryGetRequiresAssemblyFileAttribute (out AttributeData? requiresAssemblyFilesAttribute)) {
						var message = requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Message").Value.Value?.ToString ();
						message = message != null ? " " + message + "." : message;
						var url = requiresAssemblyFilesAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
						url = url != null ? " " + url : url;
						operationContext.ReportDiagnostic (Diagnostic.Create (
							s_requiresAssemblyFilesRule,
							operationContext.Operation.Syntax.GetLocation (),
							member.OriginalDefinition.ToString (),
							message,
							url));
					}
				}

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
