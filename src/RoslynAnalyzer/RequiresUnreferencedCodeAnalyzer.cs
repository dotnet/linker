﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using RoslynAnalyzer;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;

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
                    var call = (IInvocationOperation) operationContext.Operation;
					CheckMethodOrCtorCall (operationContext, call.TargetMethod, call.Syntax.GetLocation());
                }, OperationKind.Invocation);

				context.RegisterOperationAction (operationContext => {
                    var call = (IObjectCreationOperation) operationContext.Operation;
					CheckMethodOrCtorCall (operationContext, call.Constructor, call.Syntax.GetLocation());
				}, OperationKind.ObjectCreation);

				context.RegisterOperationAction (operationContext => {
					var propAccess = (IPropertyReferenceOperation) operationContext.Operation;
					var prop = propAccess.Property;
					var usageInfo = propAccess.GetValueUsageInfo (prop);
					if (usageInfo.HasFlag (ValueUsageInfo.Read) && prop.GetMethod != null) {
						CheckMethodOrCtorCall (
							operationContext,
							prop.GetMethod,
							propAccess.Syntax.GetLocation ());
					}
					if (usageInfo.HasFlag (ValueUsageInfo.Write) && prop.SetMethod != null) {
						CheckMethodOrCtorCall (
							operationContext,
							prop.SetMethod,
							propAccess.Syntax.GetLocation ());
					}
				}, OperationKind.PropertyReference);

				static void CheckMethodOrCtorCall(
					OperationAnalysisContext operationContext,
					IMethodSymbol method,
					Location location) {
                    var attributes = method.GetAttributes();

                    foreach (var attr in attributes)
                    {
                        if (attr.AttributeClass is { } attrClass &&
							IsNamedType (attrClass, "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute") &&
							attr.ConstructorArguments.Length == 1 &&
							attr.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg)
                        {
							operationContext.ReportDiagnostic (Diagnostic.Create (
								s_rule,
								location,
								ToCecilDisplayString (method),
								(string) ctorArg.Value!));
						}
					}
				}
            });
        }

		private static SymbolDisplayFormat s_fqnFormat = new SymbolDisplayFormat (
			typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces);

		/// <summary>
		///  Format a method like cecil does.
		/// </summary>
		private static string ToCecilDisplayString (IMethodSymbol m)
		{
			var methodName = m.MethodKind == MethodKind.Constructor ? ".ctor" : m.Name;
			var ns = m.ContainingNamespace.IsGlobalNamespace
				? "" 
				: m.ContainingNamespace.ToDisplayString () + ".";
			ITypeSymbol? containingType = m.ContainingType;
			string typeName = containingType.Name;
			while ((containingType = containingType.ContainingType) != null) {
				typeName = $"{containingType.Name}/{typeName}";
			}
			var paramTypes = string.Join (",", m.Parameters.Select (p => p.Type.ToDisplayString (s_fqnFormat)));
			return $"{m.ReturnType.ToDisplayString (s_fqnFormat)} {ns}{typeName}::{methodName}({paramTypes})";
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
