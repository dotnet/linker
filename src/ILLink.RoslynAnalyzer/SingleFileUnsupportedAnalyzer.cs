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
	public sealed class SingleFileUnsupportedAnalyzer : DiagnosticAnalyzer
	{
		public const string IL3002 = nameof (IL3002);

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
					var methodInvocation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = methodInvocation.TargetMethod;
					var attributes = targetMethod.GetAttributes ();

					if (attributes.FirstOrDefault (attr => attr.AttributeClass is { } attrClass &&
						attrClass.HasName ("System.Diagnostics.CodeAnalysis.SingleFileUnsupportedAttribute")) is var singleFileUnsupportedAttr &&
						singleFileUnsupportedAttr != null) {
						string? messageArgument = singleFileUnsupportedAttr.ConstructorArguments[0].Value as string;
						string? urlArgument = singleFileUnsupportedAttr.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ();
						
						operationContext.ReportDiagnostic (Diagnostic.Create (
							SingleFileUnsupportedRule,
							methodInvocation.Syntax.GetLocation (),
							targetMethod.OriginalDefinition.ToString (),
							messageArgument,
							urlArgument));
					}
				}, OperationKind.Invocation);
			});
		}
	}
}
