using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.SingleFileUnsupportedAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
    public class SingleFileUnsupportedAnalyzerTests
    {
		static Task VerifySingleFileUnsupportedAnalyzer (string source, params DiagnosticResult[] expected)
		{
			var singleFileUnsupportedAttribute = File.ReadAllText ("../../../../../test/ILLink.RoslynAnalyzer.Tests/Dependencies/SingleFileUnsupportedAttribute.cs");
			source = singleFileUnsupportedAttribute + Environment.NewLine + source;
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.PublishSingleFile),
				expected);
		}

		[Fact]
		public Task SimpleDiagnostic ()
		{
			var TestSingleFileUnsupportedOnMethod = @"
class C
{
	[System.Diagnostics.CodeAnalysis.SingleFileUnsupported (""Message from attribute"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifySingleFileUnsupportedAnalyzer (TestSingleFileUnsupportedOnMethod,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()", "Message from attribute"));
		}
    }
}
