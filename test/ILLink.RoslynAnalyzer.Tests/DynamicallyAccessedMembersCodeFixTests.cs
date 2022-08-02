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
	public class DynamicallyAccessedMembersCodeFixTests
	{
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
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
				.WithSpan(12, 3, 12, 17)
				.WithSpan(10, 16, 10, 22)
				.WithArguments("System.Type.GetMethods()",
					"t",
					"C.M(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2070_ArgsTurnOffCodeFix ()
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
			var diag = new[] {
			// /0/Test0.cs(12,3): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.NonPublicMethods' in call to 'System.Type.GetMethods(BindingFlags)'.
			// The parameter 't' of method 'C.M(Type)' does not have matching annotations.
			// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
			.WithSpan(12, 3, 12, 39)
			.WithSpan(10, 16, 10, 22)
			.WithArguments("System.Type.GetMethods(BindingFlags)",
				"t",
				"C.M(Type)",
				"'DynamicallyAccessedMemberTypes.NonPublicMethods'")};
			await VerifyDynamicallyAccessedMembersCodeFix (test, test, diag, diag);
		}

		[Fact]
		public async Task CodeFix_IL2070_GetMethodsInArg ()
		{
			var test = $$"""
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;

			namespace System
			{
				static class C 
				{
					static void Main(Type t)
					{
						DoSomethingWithMethods(t.GetMethods());
					}

					static void DoSomethingWithMethods(MethodInfo[] m)
					{
					}
				}
			}
			""";

			var fixtest = """
			using System.Reflection;
			using System.Diagnostics.CodeAnalysis;

			namespace System
			{
				static class C 
				{
					static void Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t)
					{
						DoSomethingWithMethods(t.GetMethods());
					}

					static void DoSomethingWithMethods(MethodInfo[] m)
					{
					}
				}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
				// /0/Test0.cs(10,27): warning IL2070: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
				// The parameter 't' of method 'System.C.Main(Type)' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter)
				.WithSpan(10, 27, 10, 41)
				.WithSpan(8, 20, 8, 26)
				.WithArguments("System.Type.GetMethods()",
					"t",
					"System.C.Main(Type)",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
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
				// /0/Test0.cs(13,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
				.WithSpan(13, 3, 13, 21)
				.WithSpan(9, 22, 9, 37)
				.WithArguments("System.Type.GetMethod(String)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
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
				// /0/Test0.cs(13,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter)
				.WithSpan(13, 3, 13, 21)
				.WithSpan(9, 21, 9, 36)
				.WithArguments("System.Type.GetMethod(String)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2080_MismatchFieldTargetsPublicParam_Int ()
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
				// /0/Test0.cs(13,3): warning IL2080: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
				// The field 'C.f' does not have matching annotations.
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter).WithSpan(13, 3, 13, 21).WithSpan(9, 21, 9, 36)
				.WithArguments("System.Type.GetMethod(String)",
					"C.f",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods ()
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
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
			   public virtual void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
			    // /0/Test0.cs(11,33): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
			    // don't match overridden parameter 't' of method 'Base.M(Type)'.
			    // All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
			    VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 30, 11, 31)
				.WithArguments("t", "C.M(Type)",
					"t",
					"Base.M(Type)") },
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2092_MismatchMethodParamBtOverride_NonPublicMethods_Reverse () // issue: diagnostic is reported as context.ReportDiagnostic in the Analyzer (not in the Action)
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
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			public class Base
			{
				public virtual void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}
			}

			public class C : Base
			{
				public override void M([DynamicallyAccessedMembers(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods)] Type t) {}

				public static void Main() {

				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
			    // /0/Test0.cs(11,140): warning IL2092: 'DynamicallyAccessedMemberTypes' in 'DynamicallyAccessedMembersAttribute' on the parameter 't' of method 'C.M(Type)'
			    // don't match overridden parameter 't' of method 'Base.M(Type)'.
			    // All overridden members must have the same 'DynamicallyAccessedMembersAttribute' usage.
			    VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchOnMethodParameterBetweenOverrides)
				.WithSpan(11, 140, 11, 141)
				.WithArguments("t", "C.M(Type)",
					"t",
					"Base.M(Type)") },
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2068_MismatchParamTargetsMethodReturn ()
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

			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class C
			{
				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
				Type M([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type t) {
					return t;
			}
			}
			""";

			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
			    // /0/Test0.cs(8,10): warning IL2068: 'C.M(Type)' method return value does not satisfy 'DynamicallyAccessedMemberTypes.All' requirements. The parameter 't' of method 'C.M(Type)' does not have matching annotations. The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType)
			.WithSpan (8, 10, 8, 11)
			.WithSpan (7, 9, 7, 15)
			.WithArguments ("C.M(Type)",
					"t",
					"C.M(Type)", "'DynamicallyAccessedMemberTypes.All'")},
			    fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2069_MismatchParamTargetsField_PublicMethods ()
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

			var fixtest = $$"""
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
				private static Type f = typeof(Foo);
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
			    // /0/Test0.cs(17,3): warning IL2069: value stored in field 'C.f' does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. 
				//The parameter 'type' of method 'C.M(Type)' does not have matching annotations. 
				//The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField)
					.WithSpan (17, 3, 17, 11)
					.WithSpan (15, 24, 15, 33)
					.WithArguments ("C.f",
						"type",
						"C.M(Type)",
						"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());
		}

		[Fact]
		public async Task CodeFix_IL2075_MethodReturnTargetsParam_PublicMethods ()
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
					GetFoo().GetMethod("Bar");
				}

				private static Type GetFoo ()
				{
					return typeof (Foo);
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
				public static void Main()
				{
					GetFoo().GetMethod("Bar");
				}

			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    private static Type GetFoo ()
				{
					return typeof (Foo);
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
			  // /0/Test0.cs(12,3): warning IL2075: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethod(String)'.
			  //The return value of method 'C.GetFoo()' does not have matching annotations.
			  //The source value must declare at least the same requirements as those declared on the target location it is assigned to.
			 VerifyCS.Diagnostic (DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter)
			 .WithSpan (12, 3, 12, 28)
				.WithSpan (15, 2, 18, 3)
				.WithArguments ("System.Type.GetMethod(String)",
					"C.GetFoo()",
					"'DynamicallyAccessedMemberTypes.PublicMethods'")},
			    fixedExpected: Array.Empty<DiagnosticResult>());
		}

		[Fact]
		public async Task CodeFix_IL2073 ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class Foo {}

			class C {
				Type Main(Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M() {
					return Main(typeof(Foo));
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;

			class Foo {}

			class C {
			    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
			    Type Main([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] Type t) {
					return t;
				}

				[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
				Type M() {
					return Main(typeof(Foo));
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new [] {
				// /0/Test0.cs(13,10): warning IL2073: 'C.M()' method return value does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' requirements. 
				// The return value of method 'C.Main(Type)' does not have matching annotations. 
				// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
				VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType)
					.WithSpan(13, 10, 13, 27)
					.WithSpan(7, 2, 9, 3)
					.WithArguments("C.M()", 
						"C.Main(Type)", 
						"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> (), 2);

		}

		[Fact]
		public async Task CodeFix_IL2090 ()
		{
			var test = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C<T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			var fixtest = $$"""
			using System;
			using System.Diagnostics.CodeAnalysis;
			
			class C<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T> {

				void M() 
				{
					typeof(T).GetMethods();
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest,
				new[] {
					// /0/Test0.cs(8,3): warning IL2090: 'this' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'System.Type.GetMethods()'.
					// The generic parameter 'T' of 'C<T>' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsThisParameter)
						.WithSpan(8, 3, 8, 25)
						.WithSpan(4, 9, 4, 10)
						.WithArguments("System.Type.GetMethods()", 
							"T", 
							"C<T>", 
							"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());

		}


		[Fact]
		public async Task CodeFix_IL2087 ()
		{
			var test = $$"""
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
			var fixtest = $$"""
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

				private static void M2<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)] T>()
				{
					M1(typeof(T));
				}
			}
			""";
			await VerifyDynamicallyAccessedMembersCodeFix (test, fixtest, new[] {
					// /0/Test0.cs(18,3): warning IL2087: 'type' argument does not satisfy 'DynamicallyAccessedMemberTypes.PublicMethods' in call to 'C.M1(Type)'.
					// The generic parameter 'T' of 'C.M2<T>()' does not have matching annotations.
					// The source value must declare at least the same requirements as those declared on the target location it is assigned to.
					VerifyCS.Diagnostic(DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter)
						.WithSpan(18, 3, 18, 16)
						.WithSpan(16, 25, 16, 26)
						.WithArguments("type", 
							"C.M1(Type)", 
							"T", 
							"C.M2<T>()", 
							"'DynamicallyAccessedMemberTypes.PublicMethods'")},
				fixedExpected: Array.Empty<DiagnosticResult> ());

		}

	}
}