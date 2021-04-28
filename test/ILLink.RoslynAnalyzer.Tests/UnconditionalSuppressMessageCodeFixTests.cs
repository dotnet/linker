// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CodeFixes;
using VerifyCSUSMwithRUC = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer,
	ILLink.CodeFix.UnconditionalSuppressMessageCodeFixProvider>;
using VerifyCSUSMwithRAF = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.RequiresAssemblyFilesAnalyzer,
	ILLink.CodeFix.UnconditionalSuppressMessageCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class UnconditionalSuppressMessageCodeFixTests
	{
		private const string rucDef = @"
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
}";
		private const string rafDef = @"
#nullable enable
namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class RequiresAssemblyFilesAttribute : Attribute
	{
		public RequiresAssemblyFilesAttribute () { }
		public string? Message { get; set; }
		public string? Url { get; set; }
	}
}";
		private const string usmDef = @"
namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute (string category, string checkId)
		{
			Category = category;
			CheckId = checkId;
		}
		public string Category { get; }
		public string CheckId { get; }
		public string? Scope { get; set; }
		public string? Target { get; set; }
		public string? MessageId { get; set; }
		public string? Justification { get; set; }
	}
}";


		static Task VerifyUnconditionalSuppressMessageCodeFixWithRUC (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCSUSMwithRUC.Test {
				TestCode = source + rucDef + usmDef,
				FixedCode = fixedSource + rucDef + usmDef,
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableTrimAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		static Task VerifyUnconditionalSuppressMessageCodeFixWithRAF (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCSUSMwithRAF.Test {
				TestCode = source + rafDef + usmDef,
				FixedCode = fixedSource + rafDef + usmDef,
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{MSBuildPropertyOptionNames.EnableSingleFileAnalyzer} = true")));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}


		[Fact]
		public async Task RequiresUnreferencedCodeDiagnosticFix ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresUnreferencedCode(""message"")]
    public int M1() => 0;
    int M2() => M1();
}
class D
{
    public int M3(C c) => c.M1();
    public class E
    {
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        public int M5(C c) => c.M1();
    }
}
";
			var fixtest = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresUnreferencedCode(""message"")]
    public int M1() => 0;
    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    int M2() => M1();
}
class D
{
    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    public int M3(C c) => c.M1();
    public class E
    {
        [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
        public int M5(C c) => c.M1();
    }
}
";
			await VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				test, 
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRUC.Diagnostic (RequiresUnreferencedCodeAnalyzer.DiagnosticId).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", "message", ""),
				// /0/Test0.cs(11,27): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRUC.Diagnostic (RequiresUnreferencedCodeAnalyzer.DiagnosticId).WithSpan (11, 27, 11, 33).WithArguments ("C.M1()", "message", ""),
				// /0/Test0.cs(14,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRUC.Diagnostic (RequiresUnreferencedCodeAnalyzer.DiagnosticId).WithSpan (14, 31, 14, 37).WithArguments ("C.M1()", "message", ""),
				// /0/Test0.cs(21,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRUC.Diagnostic (RequiresUnreferencedCodeAnalyzer.DiagnosticId).WithSpan (21, 31, 21, 37).WithArguments ("C.M1()", "message", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task RequiresAssemblyFilesDiagnosticFix ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresAssemblyFiles(Message = ""message"")]
    public int M1() => 0;
    int M2() => M1();
}
class D
{
    public int M3(C c) => c.M1();
    public class E
    {
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        public int M5(C c) => c.M1();
    }
}
";
			var fixtest = @"
using System.Diagnostics.CodeAnalysis;
public class C
{
    [RequiresAssemblyFiles(Message = ""message"")]
    public int M1() => 0;
    [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
    int M2() => M1();
}
class D
{
    [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
    public int M3(C c) => c.M1();
    public class E
    {
        [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
        public int M5(C c) => c.M1();
    }
}
";
			await VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,17): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (7, 17, 7, 21).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(11,27): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (11, 27, 11, 33).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(14,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (14, 31, 14, 37).WithArguments ("C.M1()", " message.", ""),
				// /0/Test0.cs(21,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (21, 31, 21, 37).WithArguments ("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInSingleFileSpecialCases ()
		{
			var test = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
public class C
{
    public static Assembly assembly = Assembly.LoadFrom(""/some/path/not/in/bundle"");
    public string M1() => assembly.Location;
    public void M2() {
        _ = assembly.GetFiles();
    }
}
";
			var fixtest = @"
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
public class C
{
    public static Assembly assembly = Assembly.LoadFrom(""/some/path/not/in/bundle"");

    [UnconditionalSuppressMessage(""SingleFile"", ""IL3000:Avoid accessing Assembly file path when publishing as a single file"", Justification = ""<Pending>"")]
    public string M1() => assembly.Location;

    [UnconditionalSuppressMessage(""SingleFile"", ""IL3001:Avoid accessing Assembly file path when publishing as a single file"", Justification = ""<Pending>"")]
    public void M2() {
        _ = assembly.GetFiles();
    }
}
";
			await VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				test,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(7,27): warning IL3000: 'System.Reflection.Assembly.Location' always returns an empty string for assemblies embedded in a single-file app. If the path to the app directory is needed, consider calling 'System.AppContext.BaseDirectory'.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3000).WithSpan (7, 27, 7, 44).WithArguments ("System.Reflection.Assembly.Location", "", ""),
				// /0/Test0.cs(9,13): warning IL3001: 'System.Reflection.Assembly.GetFiles()' will throw for assemblies embedded in a single-file app
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3001).WithSpan (9, 13, 9, 32).WithArguments("System.Reflection.Assembly.GetFiles()", "", ""),
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInPropertyDecl ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    int M2 => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    int M2 => M1();
}";
			await VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSMwithRUC.Diagnostic().WithSpan(10, 15, 10, 19).WithArguments("C.M1()", "message", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInField ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresAssemblyFiles]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";
			var fixtest = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    [UnconditionalSuppressMessage(""SingleFile"", ""IL3002:Avoid calling members marked with 'RequiresAssemblyFilesAttribute' when publishing as a single-file"", Justification = ""<Pending>"")]
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresAssemblyFiles]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";
			await VerifyUnconditionalSuppressMessageCodeFixWithRAF (
				src,
				fixtest,
				baselineExpected: new[] {
				// /0/Test0.cs(6,50): warning IL3002: Using member 'C.InitC()' which has 'RequiresAssemblyFilesAttribute' can break functionality when embedded in a single-file app.
				VerifyCSUSMwithRAF.Diagnostic (RequiresAssemblyFilesAnalyzer.IL3002).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", "", ""),
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInLocalFunc ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        void Wrapper () => M1();
        return Wrapper;
    }
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			await VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSMwithRUC.Diagnostic().WithSpan(12, 28, 12, 32).WithArguments("C.M1()", "message", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInCtor ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    public C () => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    public C () => M1();
}";
			await VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSMwithRUC.Diagnostic().WithSpan(10, 20, 10, 24).WithArguments("C.M1()", "message", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task FixInEvent ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    public event EventHandler E1
    {
        add
        {
            var a = M1();
        }
        remove { }
    }
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [UnconditionalSuppressMessage(""Trimming"", ""IL2026:Methods annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code"", Justification = ""<Pending>"")]
    public event EventHandler E1
    {
        add
        {
            var a = M1();
        }
        remove { }
    }
}";
			await VerifyUnconditionalSuppressMessageCodeFixWithRUC (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(14,21): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCSUSMwithRUC.Diagnostic().WithSpan(14, 21, 14, 25).WithArguments("C.M1()", "message", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}
	}
}
