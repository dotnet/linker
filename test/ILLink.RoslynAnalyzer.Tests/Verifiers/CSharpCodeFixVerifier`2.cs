﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Testing.Verifiers;

namespace ILLink.RoslynAnalyzer.Tests
{
    /// <summary>
    /// A default verifier for diagnostic analyzers with code fixes.
    /// </summary>
    /// <typeparam name="TAnalyzer">The <see cref="DiagnosticAnalyzer"/> to test.</typeparam>
    /// <typeparam name="TCodeFix">The <see cref="CodeFixProvider"/> to test.</typeparam>
    /// <typeparam name="TTest">The test implementation to use.</typeparam>
    /// <typeparam name="XUnitVerifier">The type of verifier to use.</typeparam>
    public partial class CSharpCodeFixVerifier<TAnalyzer, TCodeFix>
           where TAnalyzer : DiagnosticAnalyzer, new()
           where TCodeFix : CodeFixProvider, new()
    {
        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, XUnitVerifier}.Diagnostic()"/>
        public static DiagnosticResult Diagnostic()
            => CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic();

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, XUnitVerifier}.Diagnostic(string)"/>
        public static DiagnosticResult Diagnostic(string diagnosticId)
            => CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic(diagnosticId);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, XUnitVerifier}.Diagnostic(DiagnosticDescriptor)"/>
        public static DiagnosticResult Diagnostic(DiagnosticDescriptor descriptor)
            => CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic(descriptor);

        /// <inheritdoc cref="AnalyzerVerifier{TAnalyzer, TTest, XUnitVerifier}.VerifyAnalyzerAsync(string, DiagnosticResult[])"/>
        public static Task VerifyAnalyzerAsync(string source, (string, string)[]? analyzerOptions = null, params DiagnosticResult[] expected)
            => CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, analyzerOptions, expected);

        /// <summary>
        /// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
        /// fixed code.
        /// </summary>
        /// <param name="source">The source text to test. Any diagnostics are defined in markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyCodeFixAsync(string source, string fixedSource)
            => VerifyCodeFixAsync(source, DiagnosticResult.EmptyDiagnosticResults, fixedSource);

        /// <summary>
        /// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
        /// fixed code.
        /// </summary>
        /// <param name="source">The source text to test, which may include markup syntax.</param>
        /// <param name="expected">The expected diagnostic. This diagnostic is in addition to any diagnostics defined in
        /// markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyCodeFixAsync(string source, DiagnosticResult expected, string fixedSource)
            => VerifyCodeFixAsync(source, new[] { expected }, fixedSource);

        /// <summary>
        /// Verifies the analyzer provides diagnostics which, in combination with the code fix, produce the expected
        /// fixed code.
        /// </summary>
        /// <param name="source">The source text to test, which may include markup syntax.</param>
        /// <param name="expected">The expected diagnostics. These diagnostics are in addition to any diagnostics
        /// defined in markup.</param>
        /// <param name="fixedSource">The expected fixed source text. Any remaining diagnostics are defined in markup.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public static Task VerifyCodeFixAsync(string source, DiagnosticResult[] expected, string fixedSource)
        {
			var test = new Test {
				TestCode = source,
				FixedCode = fixedSource,
			};

            test.ExpectedDiagnostics.AddRange(expected);
            return test.RunAsync(CancellationToken.None);
        }
    }
}
