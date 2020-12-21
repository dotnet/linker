using System;
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
			// TODO: Remove this once we have the new attribute in the runtime.
			source = @"namespace System.Diagnostics.CodeAnalysis
{
#nullable enable
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public sealed class SingleFileUnsupportedAttribute : Attribute
	{
		public SingleFileUnsupportedAttribute (string message)
		{
			Message = message;
		}

		public string Message { get; }

		public string? Url { get; set; }
	}
}" + Environment.NewLine + source;
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

		[Fact]
		public Task SingleFileUnsupportedWithMessageAndUrl ()
		{
			var TestSingleFileUnsupportedWithMessageAndUrl = @"
class C
{
	[System.Diagnostics.CodeAnalysis.SingleFileUnsupported (""Message from attribute"", Url = ""https://helpurl"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifySingleFileUnsupportedAnalyzer (TestSingleFileUnsupportedWithMessageAndUrl,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()", "Message from attribute", "https://helpurl"));
		}

		[Fact]
		public Task NoDiagnosticIfMethodNotCalled ()
		{
			var TestNoDiagnosticIfMethodNotCalled = @"
class C
{
	[System.Diagnostics.CodeAnalysis.SingleFileUnsupported ("""")]
	void M() { }
}";
			return VerifySingleFileUnsupportedAnalyzer (TestNoDiagnosticIfMethodNotCalled);
		}

		[Fact]
		public Task NoDiagnosticIsProducedIfCallerIsAnnotated ()
		{
			var TestNoDiagnosticIsProducedIfCallerIsAnnotated = @"
class C
{
	void M1()
	{
		M2();
	}

	[System.Diagnostics.CodeAnalysis.SingleFileUnsupported (""Warn from M2"")]
	void M2()
	{
		M3();
	}

	[System.Diagnostics.CodeAnalysis.SingleFileUnsupported (""Warn from M3"")]
	void M3()
	{
	}
}";
			return VerifySingleFileUnsupportedAnalyzer (TestNoDiagnosticIsProducedIfCallerIsAnnotated,
				VerifyCS.Diagnostic ().WithSpan (22, 3, 22, 7).WithArguments ("C.M2()", "Warn from M2"));
		}
    }
}
