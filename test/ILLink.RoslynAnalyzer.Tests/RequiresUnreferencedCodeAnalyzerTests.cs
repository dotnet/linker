// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
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
		static Task VerifyRequiresUnreferencedCodeAnalyzer (string source, params DiagnosticResult[] expected) =>
			VerifyRequiresUnreferencedCodeAnalyzer (source, null, expected);

		static async Task VerifyRequiresUnreferencedCodeAnalyzer (string source, IEnumerable<MetadataReference>? additionalReferences, params DiagnosticResult[] expected) =>
			await VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				additionalReferences ?? Array.Empty<MetadataReference> (),
				expected);

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
				ReferenceAssemblies = TestCaseUtils.Net6PreviewAssemblies
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
				// (7,23): warning IL2046: Member 'DerivedClass.VirtualMethod()' with 'RequiresUnreferencedCodeAttribute' overrides base member 'BaseClass.VirtualMethod()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (7, 23, 7, 36).WithArguments ("Member 'DerivedClass.VirtualMethod()' with 'RequiresUnreferencedCodeAttribute' overrides base member 'BaseClass.VirtualMethod()' without 'RequiresUnreferencedCodeAttribute'"),
				// (15,3): warning IL2046: Member 'DerivedClass.VirtualProperty.get' with 'RequiresUnreferencedCodeAttribute' overrides base member 'BaseClass.VirtualProperty.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (15, 3, 15, 6).WithArguments ("Member 'DerivedClass.VirtualProperty.get' with 'RequiresUnreferencedCodeAttribute' overrides base member 'BaseClass.VirtualProperty.get' without 'RequiresUnreferencedCodeAttribute'"));
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
				// (13,3): warning IL2046: Base member 'BaseClass.VirtualProperty.get' with 'RequiresUnreferencedCodeAttribute' has a derived member 'DerivedClass.VirtualProperty.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (13, 3, 13, 6).WithArguments ("Base member 'BaseClass.VirtualProperty.get' with 'RequiresUnreferencedCodeAttribute' has a derived member 'DerivedClass.VirtualProperty.get' without 'RequiresUnreferencedCodeAttribute'"),
				// (6,23): warning IL2046: Base member 'BaseClass.VirtualMethod()' with 'RequiresUnreferencedCodeAttribute' has a derived member 'DerivedClass.VirtualMethod()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (6, 23, 6, 36).WithArguments ("Base member 'BaseClass.VirtualMethod()' with 'RequiresUnreferencedCodeAttribute' has a derived member 'DerivedClass.VirtualMethod()' without 'RequiresUnreferencedCodeAttribute'"));
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
				// (7,14): warning IL2046: Member 'Implementation.RUC()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.RUC()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (7, 14, 7, 17).WithArguments ("Member 'Implementation.RUC()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.RUC()' without 'RequiresUnreferencedCodeAttribute'"),
				// (13,3): warning IL2046: Member 'Implementation.Property.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.Property.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (13, 3, 13, 6).WithArguments ("Member 'Implementation.Property.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.Property.get' without 'RequiresUnreferencedCodeAttribute'"),
				// (33,12): warning IL2046: Member 'ExplicitImplementation.IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.RUC()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (33, 12, 33, 15).WithArguments ("Member 'ExplicitImplementation.IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.RUC()' without 'RequiresUnreferencedCodeAttribute'"),
				// (39,3): warning IL2046: Member 'ExplicitImplementation.IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.Property.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (39, 3, 39, 6).WithArguments ("Member 'ExplicitImplementation.IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRUC.Property.get' without 'RequiresUnreferencedCodeAttribute'"));
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
				// (6,14): warning IL2046: Interface member 'IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.RUC()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (6, 14, 6, 17).WithArguments ("Interface member 'IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.RUC()' without 'RequiresUnreferencedCodeAttribute'"),
				// (11,3): warning IL2046: Interface member 'IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.Property.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (11, 3, 11, 6).WithArguments ("Interface member 'IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.Property.get' without 'RequiresUnreferencedCodeAttribute'"),
				// (18,14): warning IL2046: Interface member 'IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.RUC()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (18, 14, 18, 17).WithArguments ("Interface member 'IRUC.RUC()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.RUC()' without 'RequiresUnreferencedCodeAttribute'"),
				// (23,3): warning IL2046: Interface member 'IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.Property.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (23, 3, 23, 6).WithArguments ("Interface member 'IRUC.Property.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.Property.get' without 'RequiresUnreferencedCodeAttribute'"));
		}

		[Fact]
		public async Task MissingRAFAttributeOnSource ()
		{
			var references = @"
using System.Diagnostics.CodeAnalysis;

public interface IRAF
{
	[RequiresUnreferencedCode (""Message"")]
	void Method();
	string StringProperty { [RequiresUnreferencedCode (""Message"")] get; set; }
}";

			var src = @"
class Implementation : IRAF
{
	public void Method () { }

	private string name;
	public string StringProperty
	{
		get { return name; }
		set { name = value; }
	}
}

class AnotherImplementation : IRAF
{
	public void Method () { }

	private string name;
	public string StringProperty
	{
		get { return name; }
		set { name = value; }
	}
}
";
			var compilation = (await CSharpAnalyzerVerifier<RequiresUnreferencedCodeAnalyzer>.GetCompilation (references)).EmitToImageReference ();

			await VerifyRequiresUnreferencedCodeAnalyzer (src, additionalReferences: new[] { compilation },
				// (4,14): warning IL2046: Interface member 'IRAF.Method()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.Method()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (4, 14, 4, 20).WithArguments ("Interface member 'IRAF.Method()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.Method()' without 'RequiresUnreferencedCodeAttribute'"),
				// (16,14): warning IL2046: Interface member 'IRAF.Method()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.Method()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (16, 14, 16, 20).WithArguments ("Interface member 'IRAF.Method()' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.Method()' without 'RequiresUnreferencedCodeAttribute'"),
				// (9,3): warning IL2046: Interface member 'IRAF.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (9, 3, 9, 6).WithArguments ("Interface member 'IRAF.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'Implementation.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'"),
				// (21,3): warning IL2046: Interface member 'IRAF.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (21, 3, 21, 6).WithArguments ("Interface member 'IRAF.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' has an implementation member 'AnotherImplementation.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'"));
		}

		[Fact]
		public async Task MissingRAFAttributeOnReference ()
		{
			var references = @"
public interface IRAF
{
	void Method();
	string StringProperty { get; set; }
}";

			var src = @"
using System.Diagnostics.CodeAnalysis;

class Implementation : IRAF
{
	[RequiresUnreferencedCode (""Message"")]
	public void Method () { }

	private string name;
	public string StringProperty
	{
		[RequiresUnreferencedCode (""Message"")]
		get { return name; }
		set { name = value; }
	}
}

class AnotherImplementation : IRAF
{
	public void Method () { }

	private string name;
	public string StringProperty
	{
		get { return name; }
		set { name = value; }
	}
}
";
			var compilation = (await CSharpAnalyzerVerifier<RequiresUnreferencedCodeAnalyzer>.GetCompilation (references)).EmitToImageReference ();

			await VerifyRequiresUnreferencedCodeAnalyzer (src, additionalReferences: new[] { compilation },
				// (7,14): warning IL2046: Member 'Implementation.Method()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRAF.Method()' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (7, 14, 7, 20).WithArguments ("Member 'Implementation.Method()' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRAF.Method()' without 'RequiresUnreferencedCodeAttribute'"),
				// (13,3): warning IL2046: Member 'Implementation.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRAF.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'. Attributes must match across all interface implementations or overrides.
				VerifyCS.Diagnostic (RequiresUnreferencedCodeAnalyzer.IL2046).WithSpan (13, 3, 13, 6).WithArguments ("Member 'Implementation.StringProperty.get' with 'RequiresUnreferencedCodeAttribute' implements interface member 'IRAF.StringProperty.get' without 'RequiresUnreferencedCodeAttribute'"));
		}
	}
}
