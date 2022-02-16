// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.COMAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class ComAnalyzerTests
	{
		static Task VerifyComAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected: expected);
		}

		[Fact]
		public Task Issue ()
		{
			var TargetParameterWithAnnotations = @"
using System.Runtime.InteropServices;

static class Program
{
    [DllImport(""Unimportant"")]
    static extern unsafe void WhateverName(void* pointerArg);

    static void Main()
    {
        unsafe
        {
            WhateverName(null);
        }
    }
}";
			return VerifyComAnalyzer (TargetParameterWithAnnotations,
				DiagnosticResult.CompilerError ("CS0227").WithSpan (7, 31, 7, 43),
				DiagnosticResult.CompilerError ("CS0227").WithSpan (11, 9, 11, 15));
;
		}
	}
}
