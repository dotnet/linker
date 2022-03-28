// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresInCompilerGeneratedCode
	{
		public static void Main ()
		{
			WarnInIteratorBody.Test ();
			SuppressInIteratorBody.Test ();

			WarnInAsyncBody.Test ();
			SuppressInAsyncBody.Test ();

			WarnInAsyncIteratorBody.Test ();
			SuppressInAsyncIteratorBody.Test ();

			WarnInLocalFunction.Test ();
			SuppressInLocalFunction.Test ();

			WarnInLambda.Test ();
			SuppressInLambda.Test ();

			WarnInComplex.Test ();
			SuppressInComplex.Test ();

			StateMachinesOnlyReferencedViaReflection.Test ();

			ComplexCases.AsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncBodyCallingMethodWithRequires.Test ();
			ComplexCases.GenericAsyncEnumerableBodyCallingRequiresWithAnnotations.Test ();
		}

		class WarnInIteratorBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static IEnumerable<int> TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static IEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static IEnumerable<int> TestLazyDelegate ()
			{
				yield return 0;
				yield return 1;
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestCall ()
			{
				MethodWithRequires ();
				yield return 0;
				MethodWithRequires ();
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static IEnumerable<int> TestLazyDelegate ()
			{
				yield return 0;
				yield return 1;
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async void TestCallBeforeYieldReturn ()
			{
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async void TestCallAfterYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async void TestLazyDelegate ()
			{
				await MethodAsync ();
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestCall ()
			{
				MethodWithRequires ();
				await MethodAsync ();
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async void TestLazyDelegate ()
			{
				await MethodAsync ();
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncIteratorBody
		{
			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async IAsyncEnumerable<int> TestCallBeforeYieldReturn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async IAsyncEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				await MethodAsync ();
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				yield return 0;
				var action = new Action (MethodWithRequires);
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async IAsyncEnumerable<int> TestLazyDelegate ()
			{
				await MethodAsync ();
				yield return 0;
				_ = _default.Value;
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true, ProducedBy = ProducedBy.Trimmer)]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestCall ()
			{
				MethodWithRequires ();
				await MethodAsync ();
				yield return 0;
				MethodWithRequires ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestReflectionAccess ()
			{
				await MethodAsync ();
				yield return 0;
				typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (MethodWithRequires);
				yield return 0;
			}

			// Cannot annotate fields either with RUC nor RAF therefore the warning persists
			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static async IAsyncEnumerable<int> TestLazyDelegate ()
			{
				await MethodAsync ();
				yield return 0;
				_ = _default.Value;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				yield return 0;
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				yield return 0;
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async IAsyncEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInLocalFunction
		{
			static void TestCall ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction () => MethodWithRequires ();
			}

			static void TestCallUnused ()
			{
				// Analyzer emits warnings for code in unused local functions.
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction () => MethodWithRequires ();
			}

			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			static void TestCallWithClosureUnused (int p = 0)
			{
				// Analyzer emits warnings for code in unused local functions.
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			static void TestReflectionAccess ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			static void TestLdftn ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					_ = _default.Value;
				}
			}

			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestCall ();
				TestCallUnused ();
				TestCallWithClosure ();
				TestCallWithClosureUnused ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLocalFunction
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					MethodWithRequires ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresInCompilerGeneratedCode)
					.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (MethodWithRequires);
				}
			}

			[ExpectedWarning ("IL2026", "Message from --MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "Message from --MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					_ = _default.Value;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericLocalFunction<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
					typeof (T).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericLocalFunctionInner<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<TSecond> ()
				{
					typeof (TUnknown).RequiresPublicMethods ();
					typeof (TSecond).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotations<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> ()
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			static void TestGenericLocalFunctionWithAnnotationsAndClosure<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TPublicMethods> (int p = 0)
			{
				LocalFunction<TPublicMethods> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] TInnerPublicMethods> ()
				{
					p++;
					typeof (TPublicMethods).RequiresPublicMethods ();
					typeof (TInnerPublicMethods).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallMethodWithRequiresInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				void LocalFunction () => MethodWithRequires ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					LocalFunction ();

					void LocalFunction () => MethodWithRequires ();
				}
			}

			class DynamicallyAccessedLocalFunctionUnused
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				public static void TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunctionUnused).RequiresNonPublicMethods ();

					// This local function is unused except for the dynamic reference above,
					// so the linker isn't able to figure out which user method it belongs to,
					// and the warning is not suppressed.
					[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
			static void TestSuppressionLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the local function has Requires on it

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLocalFunction ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			class TestSuppressionOnOuterWithSameName
			{
				[ExpectedWarning ("IL2026", nameof (Outer) + "()")]
				[ExpectedWarning ("IL3002", nameof (Outer) + "()", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", nameof (Outer) + "()", ProducedBy = ProducedBy.Analyzer)]
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					LocalFunction ();

					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
					[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
					void LocalFunction () => MethodWithRequires ();
				}
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestMethodParameterWithRequirements ();
				TestDynamicallyAccessedMethod ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestGenericLocalFunctionWithAnnotations<TestType> ();
				TestGenericLocalFunctionWithAnnotationsAndClosure<TestType> ();
				TestCallMethodWithRequiresInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				DynamicallyAccessedLocalFunctionUnused.TestCallMethodWithRequiresInDynamicallyAccessedLocalFunction ();
				TestSuppressionLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
				TestSuppressionOnOuterWithSameName.Test ();
			}
		}

		class WarnInLambda
		{
			static void TestCall ()
			{
				Action lambda =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				() => MethodWithRequires ();

				lambda ();
			}

			static void TestCallUnused ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				() => MethodWithRequires ();
			}

			static void TestCallWithClosure (int p = 0)
			{
				Action lambda =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				() => {
					p++;
					MethodWithRequires ();
				};

				lambda ();
			}

			static void TestCallWithClosureUnused (int p = 0)
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				() => {
					p++;
					MethodWithRequires ();
				};
			}

			static void TestReflectionAccess ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
				() => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			static void TestLdftn ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				() => {
					var action = new Action (MethodWithRequires);
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				Action _ = () => {
					var action = _default.Value;
				};
			}

			static void TestDynamicallyAccessedMethod ()
			{
				Action _ =
				[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", ProducedBy = ProducedBy.Trimmer)]
				() => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				};
			}

			public static void Test ()
			{
				TestCall ();
				TestCallUnused ();
				TestCallWithClosure ();
				TestCallWithClosureUnused ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLambda
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCall ()
			{
				Action _ =
				() => MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithReflectionAnalysisWarning ()
			{
				// This should not produce warning because the Requires
				Action<Type> _ =
				(t) => t.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ =
				() => {
					p++;
					MethodWithRequires ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestReflectionAccess ()
			{
				Action _ =
				() => {
					typeof (RequiresInCompilerGeneratedCode)
						.GetMethod ("MethodWithRequires", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestLdftn ()
			{
				Action _ =
				() => {
					var action = new Action (MethodWithRequires);
				};
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequiresAndReturns--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequiresAndReturns--", ProducedBy = ProducedBy.Analyzer)]
			public static Lazy<string> _default = new Lazy<string> (MethodWithRequiresAndReturns);

			static void TestLazyDelegate ()
			{
				Action _ = () => {
					var action = _default.Value;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ =
				() => {
					typeof (TypeWithMethodWithRequires).RequiresNonPublicMethods ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ =
				// TODO: Fix the discrepancy between linker and analyzer
				// https://github.com/dotnet/linker/issues/2350
				// [ExpectedWarning ("IL2077", ProducedBy = ProducedBy.Trimmer)]
				// TODO: add a separate testcase for this.
				() => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ =
				() => {
					MethodWithGenericWhichRequiresMethods<TUnknown> ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				};
			}

			[ExpectedWarning ("IL2026")]
			[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
			static void TestSuppressionOnLambda ()
			{
				var lambda =
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				() => MethodWithRequires ();

				lambda (); // This will produce a warning since the lambda has Requires on it
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLambda ()
			{
				var lambda =
				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				(Type unknownType) => {
					MethodWithRequires ();
					unknownType.RequiresNonPublicMethods ();
				};

				lambda (null);
			}

			class TestSuppressionOnOuterWithSameName
			{
				[ExpectedWarning ("IL2026", nameof (Outer) + "()")]
				[ExpectedWarning ("IL3002", nameof (Outer) + "()", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", nameof (Outer) + "()", ProducedBy = ProducedBy.Analyzer)]
				public static void Test ()
				{
					Outer ();
					Outer (0);
				}

				[RequiresUnreferencedCode ("Suppress in body")]
				[RequiresAssemblyFiles ("Suppress in body")]
				[RequiresDynamicCode ("Suppress in body")]
				static void Outer ()
				{
					// Even though this method has the same name as Outer(int i),
					// it should not suppress warnings originating from compiler-generated
					// code for the lambda contained in Outer(int i).
				}

				static void Outer (int i)
				{
					var lambda =
					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
					[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
					() => MethodWithRequires ();

					lambda ();
				}
			}


			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithReflectionAnalysisWarning ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestLazyDelegate ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestSuppressionOnLambda ();
				TestSuppressionOnOuterAndLambda ();
				TestSuppressionOnOuterWithSameName.Test ();
			}
		}

		class WarnInComplex
		{
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithMethodWithRequires.MethodWithRequires--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				yield return 1;
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
			}

			public static void Test ()
			{
				TestIteratorLocalFunctionInAsync ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
		}

		class SuppressInComplex
		{

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestIteratorLocalFunction ()
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLocalFunction ()
			{
				LocalFunction ();

				async Task<int> LocalFunction ()
				{
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestIteratorLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					p++;
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				async Task<int> LocalFunction ()
				{
					p++;
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestCallToLocalFunctionInIteratorLocalFunctionWithClosure (int p = 0)
			{
				LocalFunction ();

				IEnumerable<int> LocalFunction ()
				{
					p++;
					yield return 0;
					LocalFunction2 ();
					yield return 1;

					void LocalFunction2 ()
					{
						MethodWithRequires ();
					}
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLambda ()
			{
				Func<Task<int>> _ = async Task<int> () => {
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestAsyncLambdaWithClosure (int p = 0)
			{
				Func<Task<int>> _ = async Task<int> () => {
					p++;
					await MethodAsync ();
					MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static void TestLambdaInAsyncLambdaWithClosure (int p = 0)
			{
				Func<Task<int>> _ = async Task<int> () => {
					p++;
					await MethodAsync ();
					var lambda = () => MethodWithRequires ();
					return 1;
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				[RequiresUnreferencedCode ("Suppress in local function")]
				[RequiresAssemblyFiles ("Suppress in local function")]
				[RequiresDynamicCode ("Suppress in local function")]
				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static async void TestIteratorLocalFunctionInAsyncWithoutInner ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					MethodWithRequires ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			[RequiresAssemblyFiles ("Suppress in body")]
			[RequiresDynamicCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithMethodWithRequires> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			[UnconditionalSuppressMessage ("SingleFile", "IL3002")]
			[UnconditionalSuppressMessage ("AOT", "IL3050")]
			public static void Test ()
			{
				TestIteratorLocalFunction ();
				TestAsyncLocalFunction ();
				TestIteratorLocalFunctionWithClosure ();
				TestAsyncLocalFunctionWithClosure ();
				TestCallToLocalFunctionInIteratorLocalFunctionWithClosure ();
				TestAsyncLambda ();
				TestAsyncLambdaWithClosure ();
				TestLambdaInAsyncLambdaWithClosure ();
				TestIteratorLocalFunctionInAsync ();
				TestIteratorLocalFunctionInAsyncWithoutInner ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
		}

		class StateMachinesOnlyReferencedViaReflection
		{
			[RequiresUnreferencedCode ("Requires to suppress")]
			[RequiresAssemblyFiles ("Requires to suppress")]
			[RequiresDynamicCode ("Requires to suppress")]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[RequiresUnreferencedCode ("Requires to suppress")]
			[RequiresAssemblyFiles ("Requires to suppress")]
			[RequiresDynamicCode ("Requires to suppress")]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldSuppress ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
			static IEnumerable<int> TestIteratorOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", ProducedBy = ProducedBy.Analyzer)]
			static async void TestAsyncOnlyReferencedViaReflectionWhichShouldWarn ()
			{
				await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2026", "Requires to suppress")]
			[ExpectedWarning ("IL2026", "Requires to suppress")]
			public static void Test ()
			{
				// This is not a 100% reliable test, since in theory it can be marked in any order and so it could happen that the
				// user method is marked before the nested state machine gets marked. But it's the best we can do right now.
				// (Note that currently linker will mark the state machine first actually so the test is effective).
				typeof (StateMachinesOnlyReferencedViaReflection).RequiresAll ();
			}
		}

		class ComplexCases
		{
			public class AsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				[RequiresAssemblyFiles ("")]
				[RequiresDynamicCode ("")]
				static Task<object> MethodWithRequiresAsync (Type type)
				{
					return Task.FromResult (new object ());
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static async Task<object> AsyncMethodCallingRequires (Type type)
				{
					using (var diposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync (type);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				public static void Test ()
				{
					AsyncMethodCallingRequires (typeof (object));
				}
			}

			public class GenericAsyncBodyCallingMethodWithRequires
			{
				[RequiresUnreferencedCode ("")]
				[RequiresAssemblyFiles ("")]
				[RequiresDynamicCode ("")]
				static ValueTask<TValue> MethodWithRequiresAsync<TValue> ()
				{
					return ValueTask.FromResult (default (TValue));
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static async Task<T> AsyncMethodCallingRequires<T> ()
				{
					using (var disposable = await GetDisposableAsync ()) {
						return await MethodWithRequiresAsync<T> ();
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				public static void Test ()
				{
					AsyncMethodCallingRequires<object> ();
				}
			}

			public class GenericAsyncEnumerableBodyCallingRequiresWithAnnotations
			{
				class RequiresOnCtor
				{
					[RequiresUnreferencedCode ("")]
					[RequiresAssemblyFiles ("")]
					[RequiresDynamicCode ("")]
					public RequiresOnCtor ()
					{
					}
				}

				[RequiresUnreferencedCode ("ParentSuppression")]
				[RequiresAssemblyFiles ("ParentSuppression")]
				[RequiresDynamicCode ("ParentSuppression")]
				static IAsyncEnumerable<TValue> AsyncEnumMethodCallingRequires<
					[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicProperties)] TValue> ()
				{
					return CreateAsync ();

					[RequiresUnreferencedCode ("")]
					[RequiresAssemblyFiles]
					[RequiresDynamicCode ("")]
					static async IAsyncEnumerable<TValue> CreateAsync ()
					{
						await MethodAsync ();
						new RequiresOnCtor ();
						yield return default (TValue);
					}
				}

				[ExpectedWarning ("IL2026", "ParentSuppression")]
				[ExpectedWarning ("IL3002", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "ParentSuppression", ProducedBy = ProducedBy.Analyzer)]
				public static void Test ()
				{
					AsyncEnumMethodCallingRequires<object> ();
				}
			}

			class Disposable : IDisposable { public void Dispose () { } }

			static Task<Disposable> GetDisposableAsync () { return Task.FromResult (new Disposable ()); }
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		[RequiresAssemblyFiles ("--MethodWithRequires--")]
		[RequiresDynamicCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}

		class TypeWithMethodWithRequires
		{
			[RequiresUnreferencedCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresAssemblyFiles ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			[RequiresDynamicCode ("--TypeWithMethodWithRequires.MethodWithRequires--")]
			static void MethodWithRequires ()
			{
			}
		}

		[RequiresUnreferencedCode ("Message from --MethodWithRequiresAndReturns--")]
		[RequiresAssemblyFiles ("Message from --MethodWithRequiresAndReturns--")]
		[RequiresDynamicCode ("Message from --MethodWithRequiresAndReturns--")]
		public static string MethodWithRequiresAndReturns ()
		{
			return "string";
		}

		static void MethodWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> ()
		{
		}

		class TypeWithGenericWhichRequiresNonPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] T> { }

		class TestType { }
	}
}
