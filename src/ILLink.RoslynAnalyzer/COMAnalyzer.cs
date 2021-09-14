// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public sealed class COMAnalyzer : DiagnosticAnalyzer
	{
		private const string DllImportAttribute = nameof (DllImportAttribute);
		private const string MarshalAsAttribute = nameof (MarshalAsAttribute);

		static readonly DiagnosticDescriptor s_correctnessOfCOMCannotBeGuaranteed = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed,
			helpLinkUri: "https://docs.microsoft.com/en-us/dotnet/core/deploying/trim-warnings/il2050");

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_correctnessOfCOMCannotBeGuaranteed);

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var compilation = context.Compilation;
				if (!context.Options.IsMSBuildPropertyValueTrue (MSBuildPropertyOptionNames.EnableTrimAnalyzer, compilation))
					return;

				context.RegisterOperationAction (operationContext => {
					var invocationOperation = (IInvocationOperation) operationContext.Operation;
					var targetMethod = invocationOperation.TargetMethod;
					if (!targetMethod.HasAttribute (DllImportAttribute))
						return;

					foreach (var parameter in targetMethod.Parameters) {
						if (IsComInterop (parameter)) {
							operationContext.ReportDiagnostic (Diagnostic.Create (s_correctnessOfCOMCannotBeGuaranteed,
								operationContext.Operation.Syntax.GetLocation (), targetMethod.GetDisplayName ()));
						}
					}

					if (IsComInterop (targetMethod.ReturnType)) {
						operationContext.ReportDiagnostic (Diagnostic.Create (s_correctnessOfCOMCannotBeGuaranteed,
								operationContext.Operation.Syntax.GetLocation (), targetMethod.GetDisplayName ()));
					}
				}, OperationKind.Invocation);
			});

			static bool IsComInterop (ISymbol symbol)
			{
				if (symbol.TryGetAttribute (MarshalAsAttribute, out var marshalAsAttribute) &&
					marshalAsAttribute.ConstructorArguments.Length >= 1 && marshalAsAttribute.ConstructorArguments[0] is TypedConstant typedConstant &&
					typedConstant.Type != null && typedConstant.Type.IsUnmanagedType) {
					var unmanagedType = typedConstant.Type;
					switch (unmanagedType.Name) {
					case "IUnknown":
					case "IDispatch":
					case "Interface":
						return true;

					default:
						break;
					}
				}

				var namedTypeSymbol = symbol.ContainingType;
				if (namedTypeSymbol.ContainingNamespace.Name == "System" && namedTypeSymbol.Name == "Array") {
					return true;
				} else if (namedTypeSymbol.ContainingNamespace.Name == "System" && namedTypeSymbol.Name == "String" ||
					namedTypeSymbol.ContainingNamespace.Name == "System.Text" && namedTypeSymbol.Name == "StringBuilder") {
					return false;
				}

				return false;
			}
		}
	}
}
