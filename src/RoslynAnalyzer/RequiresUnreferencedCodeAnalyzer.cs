// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RoslynAnalyzer;
using System;
using System.Collections.Immutable;

namespace ILTrimmingAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RequiresUnreferencedCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "IL2026";

        private const string s_trimmingEnabledString = "PublishTrimmed";

        private static readonly LocalizableString s_title = new LocalizableResourceString(
            nameof(RequiresUnreferencedCodeAnalyzer) + "Title",
            Resources.ResourceManager,
            typeof(Resources));
        private static readonly LocalizableString s_messageFormat = new LocalizableResourceString(
            nameof(RequiresUnreferencedCodeAnalyzer) + "Message",
            Resources.ResourceManager,
            typeof(Resources));
        private const string s_category = "Trimming";

        private static DiagnosticDescriptor s_rule = new DiagnosticDescriptor(
            DiagnosticId,
            s_title,
            s_messageFormat,
            s_category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(s_rule);

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.ReportDiagnostics);

            context.RegisterCompilationStartAction(context =>
            {
                var compilation = context.Compilation;
                var isTrimmingEnabled = context.Options.GetMSBuildPropertyValue(
                    s_trimmingEnabledString, compilation, context.CancellationToken);

                if (!string.Equals(isTrimmingEnabled?.Trim(), "true", StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                context.RegisterOperationAction(operationContext =>
                {
                    var invocation = (IInvocationOperation)operationContext.Operation;
                    var method = invocation.TargetMethod;
                    var attributes = method.GetAttributes();

                    foreach (var attr in attributes)
                    {
                        if (attr.AttributeClass is { } attrClass &&
							IsNamedType(attrClass, "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute") &&
							attr.ConstructorArguments.Length == 1 &&
							attr.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg)
                        {
							operationContext.ReportDiagnostic (Diagnostic.Create (
								s_rule,
								invocation.Syntax.GetLocation (),
								method.ToDisplayString(),
								(string) ctorArg.Value!));
						}
					}
                }, OperationKind.Invocation);
            });
        }

        /// <summary>
        /// Returns true if <see paramref="type" /> has the same name as <see paramref="typename" />
        /// </summary>
        internal static bool IsNamedType(INamedTypeSymbol type, string typeName)
        {
            var roSpan = typeName.AsSpan();
            INamespaceOrTypeSymbol? currentType = type;
            while (roSpan.Length > 0)
            {
                var dot = roSpan.LastIndexOf('.');
                var currentName = dot < 0 ? roSpan : roSpan.Slice(dot+1);
                if (currentType is null ||
                    !currentName.Equals(currentType.Name.AsSpan(), StringComparison.Ordinal))
                {
                    return false;
                }
                currentType = (INamespaceOrTypeSymbol?)currentType.ContainingType ?? currentType.ContainingNamespace;
				roSpan = roSpan.Slice (0, dot > 0 ? dot : 0);
            }

            return true;
        }
    }
}
