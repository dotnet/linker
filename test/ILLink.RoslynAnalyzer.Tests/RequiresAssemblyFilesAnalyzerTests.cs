using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.RequiresAssemblyFilesAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresAssemblyFilesAnalyzerTests
	{
		static Task VerifyRequiresAssemblyFilesAnalyzer (string source, params DiagnosticResult[] expected)
		{
			// TODO: Remove this once we have the new attribute in the runtime.
			source = @"namespace System.Diagnostics.CodeAnalysis
{
#nullable enable
	[AttributeUsage (AttributeTargets.Method | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
	public sealed class RequiresAssemblyFilesAttribute : Attribute
	{
		public RequiresAssemblyFilesAttribute (string message)
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
			var TestRequiresAssemblyFilesOnMethod = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (""Message from attribute"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesOnMethod,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()", "Message from attribute"));
		}

		[Fact]
		public Task RequiresAssemblyFilesWithMessageAndUrl ()
		{
			var TestRequiresAssemblyFilesWithMessageAndUrl = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (""Message from attribute"", Url = ""https://helpurl"")]
	void M1()
	{
	}

	void M2()
	{
		M1();
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestRequiresAssemblyFilesWithMessageAndUrl,
				VerifyCS.Diagnostic ().WithSpan (27, 3, 27, 7).WithArguments ("C.M2()", "Message from attribute", "https://helpurl"));
		}

		[Fact]
		public Task NoDiagnosticIfMethodNotCalled ()
		{
			var TestNoDiagnosticIfMethodNotCalled = @"
class C
{
	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles ("""")]
	void M() { }
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIfMethodNotCalled);
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

	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (""Warn from M2"")]
	void M2()
	{
		M3();
	}

	[System.Diagnostics.CodeAnalysis.RequiresAssemblyFiles (""Warn from M3"")]
	void M3()
	{
	}
}";
			return VerifyRequiresAssemblyFilesAnalyzer (TestNoDiagnosticIsProducedIfCallerIsAnnotated,
				VerifyCS.Diagnostic ().WithSpan (22, 3, 22, 7).WithArguments ("C.M2()", "Warn from M2"));
		}
	}
}
