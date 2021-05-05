﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
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
		public const string FullyQualifiedRequiresAssemblyFilesAttribute = "System.Diagnostics.CodeAnalysis." + RequiresAssemblyFilesAttribute;

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
				var dangerousPatterns = dangerousPatternsBuilder.ToImmutable ();

				context.RegisterOperationAction (operationContext => {
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					CheckCalledMember (operationContext, methodInvocation.TargetMethod, dangerousPatterns);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var objectCreation = (IObjectCreationOperation) operationContext.Operation;
					var ctor = objectCreation.Constructor;
					if (ctor is not null) {
						CheckCalledMember (operationContext, ctor, dangerousPatterns);
					}
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var fieldAccess = (IFieldReferenceOperation) operationContext.Operation;
					if (fieldAccess.Field.ContainingType is INamedTypeSymbol { StaticConstructors: var ctors } &&
						!SymbolEqualityComparer.Default.Equals (operationContext.ContainingSymbol.ContainingType, fieldAccess.Field.ContainingType)) {
						CheckStaticConstructors (operationContext, ctors);
					}
				}, OperationKind.FieldReference);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckCalledMember (operationContext, prop.GetMethod, dangerousPatterns);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckCalledMember (operationContext, prop.SetMethod, dangerousPatterns);

					CheckCalledMember (operationContext, prop, dangerousPatterns);
				}, OperationKind.PropertyReference);

				context.RegisterOperationAction (operationContext => {
					var eventRef = (IEventReferenceOperation) operationContext.Operation;
					CheckCalledMember (operationContext, eventRef.Member, dangerousPatterns);
				}, OperationKind.EventReference);

				context.RegisterOperationAction (operationContext => {
					var delegateCreation = (IDelegateCreationOperation) operationContext.Operation;
					IMethodSymbol methodSymbol;
					if (delegateCreation.Target is IMethodReferenceOperation methodRef)
						methodSymbol = methodRef.Method;
					else if (delegateCreation.Target is IAnonymousFunctionOperation lambda)
						methodSymbol = lambda.Symbol;
					else
						return;
					CheckCalledMember (operationContext, methodSymbol, dangerousPatterns);
				}, OperationKind.DelegateCreation);

				static void CheckStaticConstructors (OperationAnalysisContext operationContext,
					ImmutableArray<IMethodSymbol> constructors)
				{
					foreach (var constructor in constructors) {
						if (constructor.Parameters.Length == 0 && constructor.HasAttribute (RequiresAssemblyFilesAttribute) && constructor.MethodKind == MethodKind.StaticConstructor) {
							if (constructor.TryGetAttributeWithMessageOnCtor (FullyQualifiedRequiresAssemblyFilesAttribute, out AttributeData? requiresUnreferencedCode)) {
								operationContext.ReportDiagnostic (Diagnostic.Create (
									s_requiresAssemblyFilesRule,
									operationContext.Operation.Syntax.GetLocation (),
									constructor.ToString (),
									(string) requiresUnreferencedCode!.ConstructorArguments[0].Value!,
									requiresUnreferencedCode!.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
							}
						}
					}
				}

				static void CheckCalledMember (
					OperationAnalysisContext operationContext,
					ISymbol member,
					ImmutableArray<ISymbol> dangerousPatterns)
				{
					// Find containing symbol
					ISymbol? containingSymbol = null;
					for (var current = operationContext.Operation;
						 current is not null;
						 current = current.Parent) {
						if (current is ILocalFunctionOperation local) {
							containingSymbol = local.Symbol;
							break;
						} else if (current is IAnonymousFunctionOperation lambda) {
							containingSymbol = lambda.Symbol;
							break;
						} else if (current is IMethodBodyBaseOperation) {
							break;
						}
					}
					containingSymbol ??= operationContext.ContainingSymbol;

					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (containingSymbol.HasAttribute (RequiresAssemblyFilesAttribute))
						return;
					// In case ContainingSymbol is a property accesor check also for RequiresAssemblyFilesAttribute in the associated property
					if (containingSymbol is IMethodSymbol methodSymbol &&
						(methodSymbol.MethodKind == MethodKind.PropertyGet || methodSymbol.MethodKind == MethodKind.PropertySet ||
						methodSymbol.MethodKind == MethodKind.EventAdd || methodSymbol.MethodKind == MethodKind.EventRemove)
						&& methodSymbol.AssociatedSymbol!.HasAttribute (RequiresAssemblyFilesAttribute)) {
						return;
					}

					if (member is IMethodSymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_getFilesRule, operationContext.Operation.Syntax.GetLocation (), member));
						return;
					} else if (member is IPropertySymbol && ImmutableArrayOperations.Contains (dangerousPatterns, member, SymbolEqualityComparer.Default)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_locationRule, operationContext.Operation.Syntax.GetLocation (), member));
						return;
					}

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
			});
		}
	}
}
