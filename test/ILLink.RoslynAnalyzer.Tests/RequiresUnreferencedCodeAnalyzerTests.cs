﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer,
	ILLink.CodeFix.RequiresUnreferencedCodeCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class RequiresUnreferencedCodeAnalyzerTests
	{
		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected);
		}

		static Task VerifyRequiresUnreferencedCodeCodeFix (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
		{
			var test = new VerifyCS.Test {
				TestCode = source,
				FixedCode = fixedSource,
				ReferenceAssemblies = ReferenceAssemblies.Net.Net50,
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

		[Fact]
		public async Task SimpleDiagnosticFix ()
		{
			var test = @"
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
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
    [RequiresUnreferencedCodeAttribute(""message"")]
    public int M1() => 0;

    [RequiresUnreferencedCode(""Calls M1"")]
    int M2() => M1();
}
class D
{
    [RequiresUnreferencedCode(""Calls M1"")]
    public int M3(C c) => c.M1();

    public class E
    {
        [RequiresUnreferencedCode(""Calls M1"")]
        public int M4(C c) => c.M1();
    }
}
public class E
{
    public class F
    {
        [RequiresUnreferencedCode()]
        public int M5(C c) => c.M1();
    }
}
";

			await VerifyRequiresUnreferencedCodeCodeFix (test, fixtest, new[] {
	// /0/Test0.cs(9,17): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (9, 17, 9, 21).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(13,27): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(13, 27, 13, 33).WithArguments("C.M1()", " message.", ""),
	// /0/Test0.cs(17,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (17, 31, 17, 37).WithArguments ("C.M1()", " message.", ""),
	// /0/Test0.cs(24,31): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
	VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (24, 31, 24, 37).WithArguments ("C.M1()", " message.", "")
			}, new[] {
	// /0/Test0.cs(27,10): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
	DiagnosticResult.CompilerError("CS7036").WithSpan(27, 10, 27, 36).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
			}
	);
		}

		[Fact]
		public Task FixInLambda ()
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
        return () => M1();
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
        return () => M1();
    }
}";
			// No fix available inside a lambda, requries manual code change since attribute cannot
			// be applied
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,22): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(12, 22, 12, 26).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task FixInLocalFunc ()
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

    [RequiresUnreferencedCode(""Calls Wrapper"")]
    Action M2()
    {
        [global::System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute(""Calls M1"")] void Wrapper () => M1();
        return Wrapper;
    }
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(12,28): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(12, 28, 12, 32).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: Array.Empty<DiagnosticResult> (),
				// The default iterations for the codefix is the number of diagnostics (1 in this case)
				// but since the codefixer introduces a new diagnostic in the first iteration, it needs
				// to run twice, so we need to set the number of iterations to 2.
				numberOfIterations: 2);
		}

		[Fact]
		public Task FixInCtor ()
		{
			var src = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public static int M1() => 0;

    public C() => M1();
}";
			var fix = @"
using System;
using System.Diagnostics.CodeAnalysis;

public class C
{
    [RequiresUnreferencedCodeAttribute(""message"")]
    public static int M1() => 0;

    [RequiresUnreferencedCode()]
    public C() => M1();
}";
			// Roslyn currently doesn't simplify the attribute name properly, see https://github.com/dotnet/roslyn/issues/52039
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,19): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(10, 19, 10, 23).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,6): error CS7036: There is no argument given that corresponds to the required formal parameter 'message' of 'RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)'
					DiagnosticResult.CompilerError("CS7036").WithSpan(10, 6, 10, 32).WithArguments("message", "System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute.RequiresUnreferencedCodeAttribute(string)"),
				});
		}

		[Fact]
		public Task FixInPropertyDecl ()
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

    int M2 => M1();
}";
			// Can't apply RUC on properties at the moment
			return VerifyRequiresUnreferencedCodeCodeFix (
				src,
				fix,
				baselineExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				},
				fixedExpected: new[] {
					// /0/Test0.cs(10,15): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. message.
					VerifyCS.Diagnostic(RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan(10, 15, 10, 19).WithArguments("C.M1()", " message.", "")
				});
		}

		[Fact]
		public Task TestTrailingPeriodsOnWarningMessageAreNotDupplicated ()
		{
			var source = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[RequiresUnreferencedCode (""Warning message"")]
	static void MessageWithoutTrailingPeriod ()
	{
	}

	[RequiresUnreferencedCode (""Warning message."")]
	static void MessageWithTrailingPeriod ()
	{
	}

	static void Test ()
	{
		MessageWithoutTrailingPeriod ();
		MessageWithTrailingPeriod ();
	}
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (source,
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (18, 3, 18, 34).WithArguments ("C.MessageWithoutTrailingPeriod()", " Warning message.", string.Empty),
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (19, 3, 19, 31).WithArguments ("C.MessageWithTrailingPeriod()", " Warning message.", string.Empty));
		}

		[Fact]
		public Task LazyDelegateWithRequiresUnreferencedCode ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    public static Lazy<C> _default = new Lazy<C>(InitC);
    public static C Default => _default.Value;

    [RequiresUnreferencedCode (""Message from --C.InitC--"")]
    public static C InitC() {
        C cObject = new C();
        return cObject;
    }
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (6,50): warning IL2026: Using method 'C.InitC()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.InitC--.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (6, 50, 6, 55).WithArguments ("C.InitC()", " Message from --C.InitC--.", ""));
		}

		[Fact]
		public Task ActionDelegateWithRequiresAssemblyFiles ()
		{
			const string src = @"
using System;
using System.Diagnostics.CodeAnalysis;
class C
{
    [RequiresUnreferencedCode (""Message from --C.M1--"")]
    public static void M1() { }
    public static void M2()
    {
        Action a = M1;
        Action b = () => M1();
    }
}";

			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (10,20): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.M1--.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (10, 20, 10, 22).WithArguments ("C.M1()", " Message from --C.M1--.", ""),
				// (11,26): warning IL2026: Using method 'C.M1()' which has 'RequiresUnreferencedCodeAttribute' can break functionality when trimming application code. Message from --C.M1--.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2026).WithSpan (11, 26, 11, 30).WithArguments ("C.M1()", " Message from --C.M1--.", ""));
		}

		[Fact]
		public Task OverrideHasAttributeButBaseDoesnt ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class DerivedClass : BaseClass
{
	[RequiresUnreferencedCode(""Message"")]
	public override void VirtualMethod ()
	{
	}

	private string name;
	public override string VirtualProperty
	{
		[RequiresUnreferencedCode(""Message"")]
		get { return name; }
		set { name = value; }
	}
}

class BaseClass
{
	public virtual void VirtualMethod ()
	{
	}

	public virtual string VirtualProperty { get; set; }
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (22,22): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'DerivedClass.VirtualMethod()' doesn't match overridden method 'BaseClass.VirtualMethod()'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (22, 22, 22, 35).WithArguments ("RequiresUnreferencedCodeAttribute", "DerivedClass.VirtualMethod()", "BaseClass.VirtualMethod()"),
				// (26,42): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'DerivedClass.VirtualProperty.get' doesn't match overridden method 'BaseClass.VirtualProperty.get'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (26, 42, 26, 45).WithArguments ("RequiresUnreferencedCodeAttribute", "DerivedClass.VirtualProperty.get", "BaseClass.VirtualProperty.get"));
		}

		[Fact]
		public Task VirtualHasAttributeButOverrideDoesnt ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class DerivedClass : BaseClass
{
	public override void VirtualMethod ()
	{
	}

	private string name;
	public override string VirtualProperty
	{
		get { return name; }
		set { name = value; }
	}
}

class BaseClass
{
	[RequiresUnreferencedCode(""Message"")]
	public virtual void VirtualMethod ()
	{
	}

	public virtual string VirtualProperty {[RequiresUnreferencedCode(""Message"")] get; set; }
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (6,23): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'BaseClass.VirtualMethod()' doesn't match overridden method 'DerivedClass.VirtualMethod()'. All overridden methods must have 'RequiresAssemblyFilesAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (6, 23, 6, 36).WithArguments ("RequiresUnreferencedCodeAttribute", "BaseClass.VirtualMethod()", "DerivedClass.VirtualMethod()"),
				// (13,3): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'BaseClass.VirtualProperty.get' doesn't match overridden method 'DerivedClass.VirtualProperty.get'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (13, 3, 13, 6).WithArguments ("RequiresUnreferencedCodeAttribute", "BaseClass.VirtualProperty.get", "DerivedClass.VirtualProperty.get"));
		}

		[Fact]
		public Task ImplementationHasAttributeButInterfaceDoesnt ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class Implementation : IRUC
{
	[RequiresUnreferencedCode(""Message"")]
	public void RUC () { }

	private string name;
	public string Property
	{
		[RequiresUnreferencedCode(""Message"")]
		get { return name; }
		set { name = value; }
	}
}

class AnotherImplementation : IRUC
{
	public void RUC () { }

	private string name;
	public string Property
	{
		get { return name; }
		set { name = value; }
	}
}

class ExplicitImplementation : IRUC
{
	[RequiresUnreferencedCode(""Message"")]
	void IRUC.RUC() { }

	private string name;
	string IRUC.Property
	{
		[RequiresUnreferencedCode(""Message"")]
		get { return name; }
		set { name = value; }
	}
}

interface IRUC
{
	void RUC();
	string Property { get; set; }
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (32,7): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'Implementation.RUC()' doesn't match overridden method 'IRUC.RUC()'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (32, 7, 32, 10).WithArguments ("RequiresUnreferencedCodeAttribute", "Implementation.RUC()", "IRUC.RUC()"),
				// (33,20): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'Implementation.Property.get' doesn't match overridden method 'IRUC.Property.get'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (33, 20, 33, 23).WithArguments ("RequiresUnreferencedCodeAttribute", "Implementation.Property.get", "IRUC.Property.get"));
		}

		[Fact]
		public Task InterfaceHasAttributeButImplementationDoesnt ()
		{
			var src = @"
using System.Diagnostics.CodeAnalysis;

class Implementation : IRUC
{
	public void RUC () { }

	private string name;
	public string Property
	{
		get { return name; }
		set { name = value; }
	}
}

class AnotherImplementation : IRUC
{
	public void RUC () { }

	private string name;
	public string Property
	{
		get { return name; }
		set { name = value; }
	}
}

interface IRUC
{
	[RequiresUnreferencedCode(""Message"")]
	void RUC();
	string Property {[RequiresUnreferencedCode(""Message"")] get; set; }
}";
			return VerifyRequiresUnreferencedCodeAnalyzer (src,
				// (6,14): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'IRUC.RUC()' doesn't match overridden method 'Implementation.RUC()'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (6, 14, 6, 17).WithArguments ("RequiresUnreferencedCodeAttribute", "IRUC.RUC()", "Implementation.RUC()"),
				// (11,3): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'IRUC.Property.get' doesn't match overridden method 'Implementation.Property.get'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (11, 3, 11, 6).WithArguments ("RequiresUnreferencedCodeAttribute", "IRUC.Property.get", "Implementation.Property.get"),
				// (18,14): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'IRUC.RUC()' doesn't match overridden method 'AnotherImplementation.RUC()'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (18, 14, 18, 17).WithArguments ("RequiresUnreferencedCodeAttribute", "IRUC.RUC()", "AnotherImplementation.RUC()"),
				// (23,3): warning IL2046: Presence of 'RequiresUnreferencedCodeAttribute' on method 'IRUC.Property.get' doesn't match overridden method 'AnotherImplementation.Property.get'. All overridden methods must have 'RequiresUnreferencedCodeAttribute'.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (23, 3, 23, 6).WithArguments ("RequiresUnreferencedCodeAttribute", "IRUC.Property.get", "AnotherImplementation.Property.get"));
		}
	}
}
