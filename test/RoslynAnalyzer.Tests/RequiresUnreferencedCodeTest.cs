// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Xunit;
using VerifyCS = Analyzer1.Test.CSharpAnalyzerVerifier<
    ILTrimmingAnalyzer.RequiresUnreferencedCodeAnalyzer>;

namespace ILTrimmingAnalyzer.Test
{
    public class RequiresUnreferencedCodeTest
    {
        [Fact]
        public Task SimpleDiagnostic()
        {
            var src = @"
using System.Diagnostics.CodeAnalysis;

namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false)]
	internal sealed class RequiresUnreferencedCodeAttribute : Attribute
	{
		public RequiresUnreferencedCodeAttribute (string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string Url { get; set; }
	}
}

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;
    int M2() => M1();
}";
            return VerifyCS.VerifyAnalyzerAsync(src,
				// /0/Test0.cs(24,17): warning IL2026: Calling 'C.M1()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. 'message'
				VerifyCS.Diagnostic().WithSpan(24, 17, 24, 21).WithArguments("C.M1()", "message")
				);
        }
    }
}
