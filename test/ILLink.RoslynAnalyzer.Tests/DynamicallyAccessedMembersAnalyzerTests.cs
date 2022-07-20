// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpCodeFixVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer,
	ILLink.CodeFix.DAMCodeFixProvider>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class DynamicallyAccessedMembersAnalyzerTests
	{
		static Task VerifyDynamicallyAccessedMembersAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer),
				expected: expected);
		}

		static Task VerifyDynamicallyAccessedMembersCodeFix (
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

		#region CodeFixTests
		[Fact]
		public async Task CodeFix_IL2070_MismatchParamTargetsThisParam_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
	public static void Main()
	{
		M(typeof(C));
	}
	static void M(Type t)
	{
		t.GetMethods();
	}
}
""";
			var fixtest = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
	public static void Main()
	{
		M(typeof(C));
	}
	static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
	{
		t.GetMethods();
	}
}
""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
				 // /0/Test0.cs(12,3): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter).WithSpan(12, 3, 12, 17).WithArguments("System.Type.GetMethods()", "t", "C.M(Type)", "'DynamicallyAccessedMemberTypes.PublicMethods'") },
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		// issue with test: all methods for BindingFlags are currently being preserved, not just NonPublicMethods
		[Fact]
		public Task CodeFix_IL2070_MismatchParamTargetsParam_NonPublicMethods ()
		{
			var test = $$"""
using System;
using System.Reflection;

class C
{
	public static void Main()
	{
		M(typeof(C));
	}
	static void M(Type t)
	{
		t.GetMethods(BindingFlags.NonPublic);
	}
}
""";
			// 			var fixtest = $$"""
			// using System;
			// using System.Reflection;
			// using System.Diagnostics.CodeAnalysis;

			// class C
			// {
			// 	public static void Main()
			// 	{
			// 		M(typeof(C));
			// 	}
			// 	static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t)
			// 	{
			// 		t.GetMethods(BindingFlags.NonPublic);
			// 	}
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
			// 	// /0/Test0.cs(13,3): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			// 	VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter).WithSpan(13, 3, 13, 39).WithArguments("System.Type.GetMethods(BindingFlags)", "t", "C.M(Type)", "'DynamicallyAccessedMemberTypes.NonPublicMethods'") } ,
			// 	// /0/Test0.cs(9,3): warning IL2111: Method 'C.M(Type)' with parameters or return value with `DynamicallyAccessedMembersAttribute` is accessed via reflection. Trimmer can't guarantee availability of the requirements of the method.
			// 	new[] {VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMethodAccessedViaReflection).WithSpan(9, 3, 9, 15).WithArguments("C.M(Type)")});
		}


		[Fact]
		public async Task CodeFix_IL2080_MismatchFieldTargetsPrivateParam_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;
public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod("Bar");
	}
}
""";
			var fixtest = $$"""
using System;
using System.Diagnostics.CodeAnalysis;
public class Foo
{
}

class C
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod("Bar");
	}
}
""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
				// /0/Test0.cs(14,9): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'. The field 'C.f' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
                VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter).WithSpan(13, 9, 13, 27).WithArguments("System.Type.GetMethod(String)", "C.f", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2080_MismatchFieldTargetsPublicParam_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;
public class Foo
{
}

class C
{
    public static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod("Bar");
	}
}
""";
			var fixtest = $$"""
using System;
using System.Diagnostics.CodeAnalysis;
public class Foo
{
}

class C
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    public static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod("Bar");
	}
}
""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
				// /0/Test0.cs(14,9): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'. The field 'C.f' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
                VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter).WithSpan(13, 9, 13, 27).WithArguments("System.Type.GetMethod(String)", "C.f", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		// these diagnosticIDs are currently unsupported, and as such will currently return no CodeFixers. However, they will soon be supported and as such comments have been left to indicate the error and fix they will accomplish.

		[Fact]
		public static Task CodeFix_IL2067_MismatchparamTargetsParam_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}
""";
			// 			var fixtest = $$"""
			// using System;
			// using System.Diagnostics.CodeAnalysis;

			// public class Foo
			// {
			// }

			// class C
			// {
			// 	public static void Main()
			// 	{
			// 		M(typeof(Foo));
			// 	}

			// 	private static void NeedsPublicMethodsOnParameter(
			// 		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
			// 	{
			// 	}

			// 	private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			// 	{
			// 		NeedsPublicMethodsOnParameter(type);
			// 	}
			// }
			//""";
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(23,3): warning IL2067: 'parameter' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'. The parameter 'type' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			//     VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter).WithSpan(23, 3, 23, 38).WithArguments("parameter", "C.NeedsPublicMethodsOnParameter(Type)", "type", "C.M(Type)", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public static Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Base
{
    public virtual void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
}

public class C : Base
{
    public override void M(Type t) {}

    public static void Main() {

	}
}
""";
			// 			var fixtest = $$"""
			// using System;
			// using System.Diagnostics.CodeAnalysis;

			// public class Base
			// {
			//     public virtual void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			// }

			// public class C : Base
			// {
			//     public override void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

			//     public static void Main() {

			// 	}
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(12,33): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)' don't match overridden parameter 't' of method 'Base.M(Type)'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
			// 	VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides).WithSpan(12, 33, 12, 34).WithArguments("t", "C.M(Type)", "t", "Base.M(Type)") },
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public static Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods_Reverse ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Base
{
	public virtual void M(Type t) {}
}

public class C : Base
{
	public override void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

	public static void Main() {

	}
}
""";
			// 			var fixtest = $$"""
			// using System;
			// using System.Diagnostics.CodeAnalysis;

			// public class Base
			// {
			//     public virtual void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			// }

			// public class C : Base
			// {
			//     public override void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

			//     public static void Main() {

			// 	}
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(12,140): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)' don't match overridden parameter 't' of method 'Base.M(Type)'. All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
			// 	VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides).WithSpan(12, 140, 12, 141).WithArguments("t", "C.M(Type)", "t", "Base.M(Type)") },
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}


		[Fact]
		public static Task CodeFix_IL2067_MismatchParamTargetsParam_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}
""";
			// 			var fixtest = $$"""
			// using System;
			// using System.Diagnostics.CodeAnalysis;

			// public class Foo
			// {
			// }

			// class C
			// {
			// 	public static void Main()
			// 	{
			// 		M(typeof(Foo));
			// 	}

			// 	private static void NeedsPublicMethodsOnParameter(
			// 		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
			// 	{
			// 	}

			// 	private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			// 	{
			// 		NeedsPublicMethodsOnParameter(type);
			// 	}
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(23,3): warning IL2067: 'parameter' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'. The parameter 'type' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			//     VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter).WithSpan(23, 3, 23, 38).WithArguments("parameter", "C.NeedsPublicMethodsOnParameter(Type)", "type", "C.M(Type)", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public static Task CodeFix_IL2069_MismatchParamTargetsField_PublicMethods ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M(Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f = typeof(Foo);
}
""";

			// 			var fixtest = $$"""
			// using System;
			// using System.Diagnostics.CodeAnalysis;

			// public class Foo
			// {
			// }

			// class C
			// {
			//     public static void Main()
			//     {
			//         M(typeof(Foo));
			//     }

			//     private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
			//     {
			//         f = type;
			//     }

			//     [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			//     private static Type f = typeof(Foo);
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(18,9): warning IL2069: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. The parameter 'type' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			//     VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField).WithSpan(18, 9, 18, 17).WithArguments("C.f", "type", "C.M(Type)", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public static Task CodeFix_IL2075_MethodReturnTargetsParam_PublicMethods ()
		{
			var test = $$"""
using System;

public class Foo
{
}

class C
{
    public static void Main()
    {
        GetFoo().GetMethod(""Bar"");

	}

	private static Type GetFoo ()
	{
		return typeof (Foo);
	}
}
""";
			// 			var fixtest = $$"""
			// using System;

			// public class Foo
			// {
			// }

			// class C
			// {
			//     public static void Main()
			//     {
			//         GetFoo().GetMethod(""Bar"");

			// 	}

			//     [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			// 	private static Type GetFoo ()
			// 	{
			// 		return typeof (Foo);
			// 	}
			// }
			// """;
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(12,9): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'. The return value of method 'C.GetFoo()' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			//     VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter).WithSpan(12, 9, 12, 34).WithArguments("System.Type.GetMethod(String)", "C.GetFoo()", "'DynamicallyAccessedMemberTypes.PublicMethods'")},
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public Task CodeFix_IL2068_MismatchParamTargetsMethodReturn ()
		{
			var test = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
Type M(Type t) {
    return t;
}
}
""";
			return VerifyCS.VerifyAnalyzerAsync (test);
			// await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			// 	// /0/Test0.cs(8,12): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.All' requirements. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			// 	VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType).WithSpan(8, 12, 8, 13).WithArguments("C.M(Type)", "t", "C.M(Type)", "'DynamicallyAccessedMemberTypes.All'")},
			// 	fixedExpected: Array.Empty<DiagnosticResult> ());
		}
		#endregion

		[Fact]
		public Task NoWarningsIfAnalyzerIsNotEnabled ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}
""";
			return VerifyCS.VerifyAnalyzerAsync (TargetParameterWithAnnotations);
		}

		#region SourceParameter
		[Fact]
		public Task SourceParameterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
	public static void Main()
	{
		M(typeof(Foo));
	}

	private static void NeedsPublicMethodsOnParameter(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type parameter)
	{
	}

	private static void M(Type type)
	{
		NeedsPublicMethodsOnParameter(type);
	}
}
""";
			// (22,3): warning IL2067: 'parameter' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter)
				.WithSpan (22, 3, 22, 38)
				.WithArguments ("parameter",
					"C.NeedsPublicMethodsOnParameter(Type)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M(Type type)
    {
        return type;
    }
}
""";

			// (18,9): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType)
				.WithSpan (18, 16, 18, 20)
				.WithArguments ("C.M(Type)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M(Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f = typeof(Foo);
}
""";

			// (17,9): warning IL2069: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField)
				.WithSpan (17, 9, 17, 17)
				.WithArguments ("C.f",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceParameterDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = $$"""
using System;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M(Type type)
    {
        type.GetMethod("Bar");
	}
}
""";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (16,9): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The parameter 'type' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
				.WithSpan (16, 9, 16, 30)
				.WithArguments ("System.Type.GetMethod(String)",
					"type",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethodReturnType
		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class T
{
}

class C
{
    public static void Main()
    {
        NeedsPublicMethodsOnParameter(GetT());
    }

    private static void NeedsPublicMethodsOnParameter(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static Type GetT()
    {
        return typeof(T);
    }
}
""";

			// (12,9): warning IL2072: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethodsOnParameter(Type)'.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (12, 9, 12, 46)
				.WithArguments ("type", "C.NeedsPublicMethodsOnParameter(Type)", "C.GetT()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
    {
        return GetFoo();
    }

    private static Type GetFoo()
    {
        return typeof(Foo);
    }
}
""";

			// (18,9): warning IL2073: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType)
				.WithSpan (18, 16, 18, 24)
				.WithArguments ("C.M()", "C.GetFoo()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        f = M();
    }

    private static Type M()
    {
        return typeof(Foo);
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;
}
""";

			// (12,9): warning IL2074: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The return value of method 'C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField)
				.WithSpan (12, 9, 12, 16)
				.WithArguments ("C.f",
					"C.M()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodReturnTypeDoesNotMatchTargetMethod ()
		{
			var TargetMethodWithAnnotations = $$"""
using System;

public class Foo
{
}

class C
{
    public static void Main()
    {
        GetFoo().GetMethod("Bar");

	}

	private static Type GetFoo ()
	{
		return typeof (Foo);
	}
}
""";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (11,9): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The return value of method 'C.GetT()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
				.WithSpan (11, 9, 11, 34)
				.WithArguments ("System.Type.GetMethod(String)", "C.GetFoo()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceField
		[Fact]
		public Task SourceFieldDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        NeedsPublicMethods(f);
    }

    private static void NeedsPublicMethods(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
	{
	}
}
""";

			// (15,9): warning IL2077: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.NeedsPublicMethods(Type)'.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter)
				.WithSpan (14, 9, 14, 30)
				.WithArguments ("type",
					"C.NeedsPublicMethods(Type)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
	{
        return f;
	}
}
""";

			// (21,9): warning IL2078: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType)
				.WithSpan (20, 16, 20, 17)
				.WithArguments ("C.M()", "C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    private static Type f1 = typeof(Foo);

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f2 = typeof(Foo);

    public static void Main()
    {
        f2 = f1;
    }
}
""";
			// (17,9): warning IL2079: value stored in field 'C.f2' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The field 'C.f1' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField)
				.WithSpan (17, 9, 17, 16)
				.WithArguments ("C.f2",
					"C.f1",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceFieldDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = $$"""
using System;

public class Foo
{
}

class C
{
    private static Type f = typeof(Foo);

    public static void Main()
    {
        f.GetMethod("Bar");
	}
}
""";
			// The warning will be generated once dataflow is able to handle GetMethod intrinsic

			// (14,9): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			// The field 'C.f' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
				.WithSpan (13, 9, 13, 27)
				.WithArguments ("System.Type.GetMethod(String)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		#region SourceMethod

		static string GetSystemTypeBase ()
		{
			return $$"""
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;

namespace System
{
	public class TestSystemTypeBase : Type
	{
		public override Assembly Assembly => throw new NotImplementedException ();

		public override string AssemblyQualifiedName => throw new NotImplementedException ();

		public override Type BaseType => throw new NotImplementedException ();

		public override string FullName => throw new NotImplementedException ();

		public override Guid GUID => throw new NotImplementedException ();

		public override Module Module => throw new NotImplementedException ();

		public override string Namespace => throw new NotImplementedException ();

		public override Type UnderlyingSystemType => throw new NotImplementedException ();

		public override string Name => throw new NotImplementedException ();

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
			| DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		public override ConstructorInfo[] GetConstructors (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override object[] GetCustomAttributes (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		public override Type GetElementType ()
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents
		| DynamicallyAccessedMemberTypes.NonPublicEvents)]
		public override EventInfo GetEvent (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicEvents | DynamicallyAccessedMemberTypes.NonPublicEvents)]
		public override EventInfo[] GetEvents (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.NonPublicFields)]
		public override FieldInfo GetField (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields
			| DynamicallyAccessedMemberTypes.NonPublicFields)]
		public override FieldInfo[] GetFields (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		public override Type GetInterface (string name, bool ignoreCase)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.Interfaces)]
		public override Type[] GetInterfaces ()
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers((DynamicallyAccessedMemberTypes)0x1FFF)]
		public override MemberInfo[] GetMembers (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		public override MethodInfo[] GetMethods (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type GetNestedType (string name, BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.NonPublicNestedTypes)]
		public override Type[] GetNestedTypes (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
		public override PropertyInfo[] GetProperties (BindingFlags bindingAttr)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
		public override object InvokeMember (string name, BindingFlags invokeAttr, Binder binder, object target, object[] args, ParameterModifier[] modifiers, CultureInfo culture, string[] namedParameters)
		{
			throw new NotImplementedException ();
		}

		public override bool IsDefined (Type attributeType, bool inherit)
		{
			throw new NotImplementedException ();
		}

		protected override TypeAttributes GetAttributeFlagsImpl ()
		{
			throw new NotImplementedException ();
		}

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors
		| DynamicallyAccessedMemberTypes.NonPublicConstructors)]
		protected override ConstructorInfo GetConstructorImpl (BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
		protected override MethodInfo GetMethodImpl (string name, BindingFlags bindingAttr, Binder binder, CallingConventions callConvention, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)]
		protected override PropertyInfo GetPropertyImpl (string name, BindingFlags bindingAttr, Binder binder, Type returnType, Type[] types, ParameterModifier[] modifiers)
		{
			throw new NotImplementedException ();
		}

		protected override bool HasElementTypeImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsArrayImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsByRefImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsCOMObjectImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPointerImpl ()
		{
			throw new NotImplementedException ();
		}

		protected override bool IsPrimitiveImpl ()
		{
			throw new NotImplementedException ();
		}
	}
}
""";
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(this);
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}
""";

			// (198,16): warning IL2082: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.C.M2(Type)'.
			// The implicit 'this' argument of method 'System.C.M1()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetParameterWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter)
				.WithSpan (198, 13, 198, 21)
				.WithArguments ("type", "System.C.M2(Type)", "System.C.M1()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task ConversionOperation ()
		{
			var ConversionOperation = $$"""
namespace System
{
    class ConvertsToType
    {
        public static implicit operator Type(ConvertsToType value) => typeof (ConvertsToType);
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), ConversionOperation),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (203, 13, 203, 37)
				.WithArguments ("type", "System.C.M2(Type)", "System.ConvertsToType.implicit operator Type(ConvertsToType)", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}


		[Fact]
		public Task ConversionOperationAnnotationDoesNotMatch ()
		{
			var AnnotatedConversionOperation = $$"""
namespace System
{
    class ConvertsToType
    {
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields)]
        public static implicit operator Type(ConvertsToType value) => null;
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), AnnotatedConversionOperation),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter)
				.WithSpan (205, 13, 205, 37)
				.WithArguments ("type", "System.C.M2(Type)", "System.ConvertsToType.implicit operator Type(ConvertsToType)", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task ConversionOperationAnnotationMatches ()
		{
			var AnnotatedConversionOperation = $$"""
namespace System
{
    class ConvertsToType
    {
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
            System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        public static implicit operator Type(ConvertsToType value) => null;
    }

    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M1();
        }

        private void M1()
        {
            M2(new ConvertsToType());
        }

        private static void M2(
            [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
				System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
        {
        }
    }
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), AnnotatedConversionOperation));
		}


		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private Type M()
        {
            return this;
        }
    }
}
""";

			// (200,13): warning IL2083: 'System.C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodReturnTypeWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType)
				.WithSpan (200, 20, 200, 24)
				.WithArguments ("System.C.M()", "System.C.M()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            f = this;
        }

        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembers(
                System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)]
        private static Type f;
    }
}
""";

			// (198,13): warning IL2084: value stored in field 'System.C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetFieldWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField)
				.WithSpan (198, 13, 198, 21)
				.WithArguments ("System.C.f",
					"System.C.M()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceMethodDoesNotMatchTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = $$"""
namespace System
{
    class C : TestSystemTypeBase
    {
        public static void Main()
        {
            new C().M();
        }

        private void M()
        {
            this.GetMethods();
        }
    }
}
""";

			// (198,13): warning IL2085: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
			// The implicit 'this' argument of method 'System.C.M()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (string.Concat (GetSystemTypeBase (), TargetMethodWithAnnotations),
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter)
				.WithSpan (198, 13, 198, 30)
				.WithArguments ("System.Type.GetMethods()", "System.C.M()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}
		#endregion

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }

    private static void M2<T>()
    {
        M1(typeof(T));
    }
}
""";

			// (18,9): warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.M1(Type)'.
			// The generic parameter 'T' of 'C.M2<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter)
				.WithSpan (18, 9, 18, 22)
				.WithArguments ("type", "C.M1(Type)", "T", "C.M2<T>()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
    private static Type M<T>()
    {
        return typeof(T);
    }
}
""";

			// (15,9): warning IL2088: 'C.M<T>()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicConstructors' requirements.
			// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType)
				.WithSpan (14, 16, 14, 25)
				.WithArguments ("C.M<T>()", "T", "C.M<T>()", "'DynamicallyAccessedMemberTypes.PublicConstructors'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M<int>();
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f;

    private static void M<T>()
    {
        f = typeof(T);
    }
}
""";

			// (17,9): warning IL2089: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements.
			// The generic parameter 'T' of 'C.M<T>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField)
				.WithSpan (16, 9, 16, 22)
				.WithArguments ("C.f",
					"T",
					"C.M<T>()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceGenericParameterDoesNotMatchTargetGenericParameterAnnotations ()
		{
			var TargetGenericParameterWithAnnotations = $$"""
using System.Diagnostics.CodeAnalysis;

class C
{
    public static void Main()
    {
        M2<int>();
    }

    private static void M1<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
    {
    }

    private static void M2<S>()
    {
        M1<S>();
    }
}
""";

			// (17,9): warning IL2091: 'T' generic argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods'
			// in 'C.M1<T>()'. The generic parameter 'S' of 'C.M2<S>()' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetGenericParameterWithAnnotations,
				VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter)
				.WithSpan (16, 9, 16, 14)
				.WithArguments ("T", "C.M1<T>()", "S", "C.M2<S>()", "'DynamicallyAccessedMemberTypes.PublicMethods'"));
		}

		[Fact]
		public Task SourceTypeofFlowsIntoTargetParameterAnnotations ()
		{
			var TargetParameterWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
    }
}
""";
			return VerifyDynamicallyAccessedMembersAnalyzer (TargetParameterWithAnnotations);
		}

		[Fact]
		public Task SourceTypeofFlowsIntoTargetMethodReturnTypeAnnotation ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M();
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M()
    {
        return typeof(Foo);
    }
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsInfoTargetMethodReturnTypeAnnotations ()
		{
			var TargetMethodReturnTypeWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        return type;
    }
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodReturnTypeWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsIntoTargetFieldAnnotations ()
		{
			var TargetFieldWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        f = type;
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
    private static Type f  = typeof(Foo);
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetFieldWithAnnotations);
		}

		[Fact]
		public Task SourceParameterFlowsIntoTargetMethodAnnotations ()
		{
			var TargetMethodWithAnnotations = $$"""
using System;
using System.Diagnostics.CodeAnalysis;

public class Foo
{
}

class C
{
    public static void Main()
    {
        M(typeof(Foo));
    }

    private static void M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
    {
        type.GetMethod("Bar");
	}
}
""";

			return VerifyDynamicallyAccessedMembersAnalyzer (TargetMethodWithAnnotations);
		}

		[Fact]
		public Task MethodArgumentIsInvalidOperation ()
		{
			var Source = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					RequireAll(type);
				}

				static void RequireAll([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {}
			}
			""";

			return VerifyDynamicallyAccessedMembersAnalyzer (Source,
				DiagnosticResult.CompilerError ("CS0103").WithSpan (8, 14, 8, 18).WithArguments ("type"));
		}

		[Fact]
		public Task MethodReturnIsInvalidOperation ()
		{
			var Source = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					GetTypeWithAll ();
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				static Type GetTypeWithAll() => type;
			}
			""";

			return VerifyDynamicallyAccessedMembersAnalyzer (Source,
				DiagnosticResult.CompilerError ("CS0103").WithSpan (12, 34, 12, 38).WithArguments ("type"));
		}

		[Fact]
		public Task AssignmentSourceIsInvalidOperation ()
		{
			var Source = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					fieldRequiresAll = type;
				}

				[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				static Type fieldRequiresAll;
			}
			""";

			return VerifyDynamicallyAccessedMembersAnalyzer (Source,
				DiagnosticResult.CompilerError ("CS0103").WithSpan (8, 22, 8, 26).WithArguments ("type"));
		}

		[Fact]
		public Task AssignmentTargetIsInvalidOperation ()
		{
			var Source = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					type = GetTypeWithAll();
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				static Type GetTypeWithAll() => null;
			}
			""";

			return VerifyDynamicallyAccessedMembersAnalyzer (Source,
				DiagnosticResult.CompilerError ("CS0103").WithSpan (8, 3, 8, 7).WithArguments ("type"));
		}

		[Fact]
		public Task AssignmentTargetIsCapturedInvalidOperation ()
		{
			var Source = """
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				public static void Main()
				{
					type ??= GetTypeWithAll();
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				static Type GetTypeWithAll() => null;
			}
			""";

			return VerifyDynamicallyAccessedMembersAnalyzer (Source,
				DiagnosticResult.CompilerError ("CS0103").WithSpan (8, 3, 8, 7).WithArguments ("type"));
		}
	}
}
