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
	public class RequiresUnreferencedCodeAnalyzer : DiagnosticAnalyzer
	{
		public const string DiagnosticId = "IL2026";
		const string RequiresUnreferencedCodeAttribute = nameof (RequiresUnreferencedCodeAttribute);
		const string FullyQualifiedRequiresUnreferencedCodeAttribute = "System.Diagnostics.CodeAnalysis." + RequiresUnreferencedCodeAttribute;

		private static readonly DiagnosticDescriptor RequiresUnreferencedCodeRule = new DiagnosticDescriptor (
			DiagnosticId,
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeAnalyzerTitle),
			Resources.ResourceManager, typeof (Resources)),
			new LocalizableResourceString (nameof (Resources.RequiresUnreferencedCodeAnalyzerMessage),
			Resources.ResourceManager, typeof (Resources)),
			DiagnosticCategory.Trimming,
			DiagnosticSeverity.Warning,
			isEnabledByDefault: true);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (RequiresUnreferencedCodeRule);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;

				var isPublishTrimmed = context.Options.GetMSBuildPropertyValue (MSBuildPropertyOptionNames.PublishTrimmed, compilation);
				if (!string.Equals (isPublishTrimmed?.Trim (), "true", StringComparison.OrdinalIgnoreCase)) {
					return;
				}

				context.RegisterOperationAction (operationContext => {
					var call = (IInvocationOperation) operationContext.Operation;
					if (call.IsVirtual && call.TargetMethod.OverriddenMethod != null)
						return;

					CheckMethodOrCtorCall (operationContext, call.TargetMethod);
				}, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
					var call = (IObjectCreationOperation) operationContext.Operation;
					CheckMethodOrCtorCall (operationContext, call.Constructor);
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null)
						CheckMethodOrCtorCall (operationContext, prop.GetMethod);

					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null)
						CheckMethodOrCtorCall (operationContext, prop.SetMethod);
				}, OperationKind.PropertyReference);

				static void CheckMethodOrCtorCall (
					OperationAnalysisContext operationContext,
					IMethodSymbol method)
				{
					// If parent method contains RequiresUnreferencedCodeAttribute then we shouldn't report diagnostics for this method
					if (operationContext.ContainingSymbol is IMethodSymbol &&
						operationContext.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAttribute))
						return;

					if (method.TryGetAttributeWithMessageOnCtor (FullyQualifiedRequiresUnreferencedCodeAttribute, out AttributeData? requiresUnreferencedCode)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (
							RequiresUnreferencedCodeRule,
							operationContext.Operation.Syntax.GetLocation (),
							method.OriginalDefinition.ToString (),
							(string) requiresUnreferencedCode!.ConstructorArguments[0].Value!,
							requiresUnreferencedCode!.NamedArguments.FirstOrDefault (na => na.Key == "Url").Value.Value?.ToString ()));
					}
				}
			});
		}
	}
}
