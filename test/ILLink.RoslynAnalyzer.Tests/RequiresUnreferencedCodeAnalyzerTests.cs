// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer,
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresUnreferencedCodeAnalyzerTests
	{
		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.PublishTrimmed),
				expected);
		}

		static Task VerifyRequiresUnreferencedCodeCodeFix (string source, string fixedSource, params DiagnosticResult[] expected)
		{
			const string rucDef = @"
#nullable enable
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
    public sealed class RequiresUnreferencedCodeAttribute : Attribute
    {
        public RequiresUnreferencedCodeAttribute(string message) { Message = message; }
        public string Message { get; }
        public string? Url { get; set; }
    }
}
";
			var test = new VerifyCS.Test {
				TestCode = source + rucDef,
				FixedCode = fixedSource + rucDef,
			};
			test.ExpectedDiagnostics.AddRange (expected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@"
is_global = true
build_property.PublishTrimmed = true")));
			return test.RunAsync ();
		}

		[Fact]
		public Task SimpleDiagnostic ()
		{
			var TestRequiresWithMessageOnlyOnMethod = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;
    int M2() => M1();
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (TestRequiresWithMessageOnlyOnMethod,
				// (8,17): warning IL2026: Using method 'C.M1()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. message.
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.M1()", "message", ""));
		}

		[Fact]
		public async Task SimpleDiagnosticFix ()
		{
            var test = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;

    int M2() => M1();
}";

            var fixtest = @"
using System.Diagnostics.CodeAnalysis;

class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    int M1() => 0;

    [RequiresUnreferencedCode("""")]
    int M2() => M1();
}";

			await VerifyRequiresUnreferencedCodeCodeFix (test, fixtest,
        // /0/Test0.cs(9,17): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message. 
    VerifyCS.Diagnostic().WithSpan(9, 17, 9, 21).WithArguments("C.M1()", "message", ""));
		}

		[Fact]
		public Task TestRequiresWithMessageAndUrlOnMethod ()
		{
			var MessageAndUrlOnMethod = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresWithMessageAndUrlOnMethod ()
	{
		RequiresWithMessageAndUrl ();
	}
	[RequiresUnreferencedCode (""Message for --RequiresWithMessageAndUrl--"", Url = ""https://helpurl"")]
	static void RequiresWithMessageAndUrl ()
	{
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (MessageAndUrlOnMethod,
				// (8,3): warning IL2026: Using method 'C.RequiresWithMessageAndUrl()' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --RequiresWithMessageAndUrl--. https://helpurl
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 31).WithArguments ("C.RequiresWithMessageAndUrl()", "Message for --RequiresWithMessageAndUrl--", "https://helpurl")
				);
		}

		[Fact]
		public Task TestRequiresOnPropertyGetter ()
		{
			var PropertyRequires = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresOnPropertyGetter ()
	{
		_ = PropertyRequires;
	}

	static int PropertyRequires {
		[RequiresUnreferencedCode (""Message for --getter PropertyRequires--"")]
		get { return 42; }
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (PropertyRequires,
				// (8,7): warning IL2026: Using method 'C.PropertyRequires.get' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --getter PropertyRequires--. 
				VerifyCS.Diagnostic ().WithSpan (8, 7, 8, 23).WithArguments ("C.PropertyRequires.get", "Message for --getter PropertyRequires--", "")
				);
		}

		[Fact]
		public Task TestRequiresOnPropertySetter ()
		{
			var PropertyRequires = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	static void TestRequiresOnPropertySetter ()
	{
		PropertyRequires = 0;
	}

	static int PropertyRequires {
		[RequiresUnreferencedCode (""Message for --setter PropertyRequires--"")]
		set { }
	}
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (PropertyRequires,
				// (8,3): warning IL2026: Using method 'C.PropertyRequires.set' which has `RequiresUnreferencedCodeAttribute` can break functionality when trimming application code. Message for --setter PropertyRequires--.
				VerifyCS.Diagnostic ().WithSpan (8, 3, 8, 19).WithArguments ("C.PropertyRequires.set", "Message for --setter PropertyRequires--", "")
				);
		}
	}
}
