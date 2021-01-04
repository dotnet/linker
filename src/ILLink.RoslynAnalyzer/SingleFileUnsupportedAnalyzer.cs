// Licensed to the .NET Foundation under one or more agreements.
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
	public sealed class SingleFileUnsupportedAnalyzer : DiagnosticAnalyzer
	{
		public const string IL3002 = nameof (IL3002);
		const string SingleFileUnsupportedAttribute = nameof (SingleFileUnsupportedAttribute);
		const string FullyQualifiedSingleFileUnsupportedAttribute = "System.Diagnostics.CodeAnalysis." + SingleFileUnsupportedAttribute;

		private static readonly DiagnosticDescriptor SingleFileUnsupportedRule = new DiagnosticDescriptor (
			IL3002,
			new LocalizableResourceString (nameof (Resources.SingleFileUnsupportedTitle),
				Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.SingleFileUnsupportedMessage),
				Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.SingleFile,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (SingleFileUnsupportedRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isSingleFilePublish = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.PublishSingleFile, compilation);
				if (!string.Equals (isSingleFilePublish?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				var includesAllContent = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.IncludeAllContentForSelfExtract, compilation);
				if (string.Equals (includesAllContent?.Trim (), "true", StringComparison.OrdinalIgnoreCase))
					return;

				context.RegisterOperationAction (operationContext => {
					// Do not emit any diagnostic if caller is annotated with the attribute too.
					if (operationContext.ContainingSymbol.HasAttribute (SingleFileUnsupportedAttribute))
						return;

					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = methodInvocation.TargetMethod;
					var attributes = targetMethod.GetAttributes ();

					if (targetMethod.TryGetAttributeWithMessageOnCtor (FullyQualifiedSingleFileUnsupportedAttribute, out AttributeData? singleFileUnsupportedAttribute)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (
							SingleFileUnsupportedRule,
							methodInvocation.Syntax.GetLocation (),
							targetMethod.OriginalDefinition.ToString (),
							(string) singleFileUnsupportedAttribute?.ConstructorArguments[0].Value!,
							singleFileUnsupportedAttribute?.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
					}
				}, OperationKind.Invocation);
			});
		}
	}
}
