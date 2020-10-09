using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing.Verifiers;
using Microsoft.CodeAnalysis.Text;

namespace Analyzer1.Test
{
    public static partial class CSharpAnalyzerVerifier<TAnalyzer>
        where TAnalyzer : DiagnosticAnalyzer, new()
    {
        public class Test : CSharpAnalyzerTest<TAnalyzer, XUnitVerifier>
        {
			public string? AnalyzerConfigDocument { get; init; }

            public Test()
            {
                SolutionTransforms.Add((solution, projectId) =>
                {
                    var compilationOptions = solution.GetProject(projectId)!.CompilationOptions!;
                    compilationOptions = compilationOptions.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.SetItems(CSharpVerifierHelper.NullableWarnings));
                    solution = solution.WithProjectCompilationOptions(projectId, compilationOptions);
                    if (AnalyzerConfigDocument is { })
                    {
                        solution = solution.AddAnalyzerConfigDocument(
                            DocumentId.CreateNewId(projectId, debugName: ".editorconfig"),
                            ".editorconfig",
                            SourceText.From($"is_global = true" + Environment.NewLine + AnalyzerConfigDocument),
                            filePath: @"z:\.editorconfig");
                    }

                    return solution;
                });
            }
        }
    }
}
