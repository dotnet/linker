// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
	public class RequiresUnreferencedCodeInCompilerGeneratedCode
	{
		public static void Main ()
		{
			WarnInIteratorBody.Test ();
			SuppressInIteratorBody.Test ();

			WarnInAsyncBody.Test ();
			SuppressInAsyncBody.Test ();

			WarnInLocalFunction.Test ();
			SuppressInLocalFunction.Test ();

			WarnInLambda.Test ();
			SuppressInLambda.Test ();

			WarnInComplex.Test ();
			SuppressInComplex.Test ();
		}

		class WarnInIteratorBody
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallBeforeYieldReturn ()
			{
				RequiresUnreferencedCodeMethod ();
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestCallAfterYieldReturn ()
			{
				yield return 0;
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInIteratorBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestCall ()
			{
				RequiresUnreferencedCodeMethod ();
				yield return 0;
				RequiresUnreferencedCodeMethod ();
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestReflectionAccess ()
			{
				yield return 0;
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestLdftn ()
			{
				yield return 0;
				yield return 1;
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				yield return 0;
				yield return 1;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				yield return 0;
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInAsyncBody
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestCallBeforeYieldReturn ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestCallAfterYieldReturn ()
			{
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			public static void Test ()
			{
				TestCallBeforeYieldReturn ();
				TestCallAfterYieldReturn ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInAsyncBody
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestCall ()
			{
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
				RequiresUnreferencedCodeMethod ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				await MethodAsync ();
				typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				await MethodAsync ();
				var action = new Action (RequiresUnreferencedCodeMethod);
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				unknownType.RequiresNonPublicMethods ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				MethodWithGenericWhichRequiresMethods<TUnknown> ();
				await MethodAsync ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				await MethodAsync ();
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInLocalFunction
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					RequiresUnreferencedCodeMethod ();
				}
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (RequiresUnreferencedCodeMethod);
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLocalFunction
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				LocalFunction ();

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				LocalFunction ();

				void LocalFunction ()
				{
					p++;
					RequiresUnreferencedCodeMethod ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestReflectionAccess ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
					.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
					.Invoke (null, new object[] { });
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestLdftn ()
			{
				LocalFunction ();

				void LocalFunction ()
				{
					var action = new Action (RequiresUnreferencedCodeMethod);
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestDynamicallyAccessedMethod ()
			{
				LocalFunction ();

				void LocalFunction () => typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				LocalFunction ();

				void LocalFunction () => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => MethodWithGenericWhichRequiresMethods<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				LocalFunction ();

				void LocalFunction () => new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericLocalFunction<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
				{
					typeof (T).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericLocalFunctionInner<TUnknown> ()
			{
				LocalFunction<TUnknown> ();

				void LocalFunction<TSecond> ()
				{
					typeof (TUnknown).RequiresPublicMethods ();
					typeof (TSecond).RequiresPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallRUCMethodInLtftnLocalFunction ()
			{
				var _ = new Action (LocalFunction);

				void LocalFunction () => RequiresUnreferencedCodeMethod ();
			}

			class DynamicallyAccessedLocalFunction
			{
				[RequiresUnreferencedCode ("Suppress in body")]
				public static void TestCallRUCMethodInDynamicallyAccessedLocalFunction ()
				{
					typeof (DynamicallyAccessedLocalFunction).RequiresNonPublicMethods ();

					void LocalFunction () => RequiresUnreferencedCodeMethod ();
				}
			}

			[ExpectedWarning ("IL2026")]
			static void TestSuppressionLocalFunction ()
			{
				LocalFunction (); // This will produce a warning since the location function has RUC on it

				[RequiresUnreferencedCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestSuppressionOnOuterAndLocalFunction ()
			{
				LocalFunction ();

				[RequiresUnreferencedCode ("Suppress in body")]
				void LocalFunction (Type unknownType = null)
				{
					RequiresUnreferencedCodeMethod ();
					unknownType.RequiresNonPublicMethods ();
				}
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestMethodParameterWithRequirements ();
				TestDynamicallyAccessedMethod ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
				TestGenericLocalFunction<TestType> ();
				TestGenericLocalFunctionInner<TestType> ();
				TestCallRUCMethodInLtftnLocalFunction ();
				DynamicallyAccessedLocalFunction.TestCallRUCMethodInDynamicallyAccessedLocalFunction ();
				TestSuppressionLocalFunction ();
				TestSuppressionOnOuterAndLocalFunction ();
			}
		}

		class WarnInLambda
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCall ()
			{
				Action _ = () => RequiresUnreferencedCodeMethod ();
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					RequiresUnreferencedCodeMethod ();
				};
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
						.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (RequiresUnreferencedCodeMethod);
				};
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				};
			}

			public static void Test ()
			{
				TestCall ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
			}
		}

		class SuppressInLambda
		{
			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCall ()
			{
				Action _ = () => RequiresUnreferencedCodeMethod ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithReflectionAnalysisWarning ()
			{
				// This should not produce warning because the RUC
				Action<Type> _ = (t) => t.RequiresPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestCallWithClosure (int p = 0)
			{
				Action _ = () => {
					p++;
					RequiresUnreferencedCodeMethod ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestReflectionAccess ()
			{
				Action _ = () => {
					typeof (RequiresUnreferencedCodeInCompilerGeneratedCode)
						.GetMethod ("RequiresUnreferencedCodeMethod", System.Reflection.BindingFlags.NonPublic)
						.Invoke (null, new object[] { });
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestLdftn ()
			{
				Action _ = () => {
					var action = new Action (RequiresUnreferencedCodeMethod);
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestDynamicallyAccessedMethod ()
			{
				Action _ = () => {
					typeof (TypeWithRUCMethod).RequiresNonPublicMethods ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static async void TestMethodParameterWithRequirements (Type unknownType = null)
			{
				Action _ = () => unknownType.RequiresNonPublicMethods ();
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericMethodParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					MethodWithGenericWhichRequiresMethods<TUnknown> ();
				};
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static void TestGenericTypeParameterRequirement<TUnknown> ()
			{
				Action _ = () => {
					new TypeWithGenericWhichRequiresNonPublicFields<TUnknown> ();
				};
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestCall ();
				TestCallWithReflectionAnalysisWarning ();
				TestCallWithClosure ();
				TestReflectionAccess ();
				TestLdftn ();
				TestDynamicallyAccessedMethod ();
				TestMethodParameterWithRequirements ();
				TestGenericMethodParameterRequirement<TestType> ();
				TestGenericTypeParameterRequirement<TestType> ();
			}
		}

		class WarnInComplex
		{
			[ExpectedWarning ("IL2026", "--RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true)]
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[ExpectedWarning ("IL2026", "--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--", CompilerGeneratedCode = true, GlobalAnalysisOnly = true)]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				yield return 1;
				MethodWithGenericWhichRequiresMethods<TypeWithRUCMethod> ();
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
			static async void TestIteratorLocalFunctionInAsync ()
			{
				await MethodAsync ();
				LocalFunction ();
				await MethodAsync ();

				IEnumerable<int> LocalFunction ()
				{
					yield return 0;
					RequiresUnreferencedCodeMethod ();
					yield return 1;
				}
			}

			[RequiresUnreferencedCode ("Suppress in body")]
			static IEnumerable<int> TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ()
			{
				MethodWithGenericWhichRequiresMethods<TypeWithRUCMethod> ();
				yield return 0;
			}

			[UnconditionalSuppressMessage ("Trimming", "IL2026")]
			public static void Test ()
			{
				TestIteratorLocalFunctionInAsync ();
				TestDynamicallyAccessedMethodViaGenericMethodParameterInIterator ();
			}
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--RequiresUnreferencedCodeMethod--")]
		static void RequiresUnreferencedCodeMethod ()
		{
		}

		class TypeWithRUCMethod
		{
			[RequiresUnreferencedCode ("--TypeWithRUCMethod.RequiresUnreferencedCodeMethod--")]
			static void RequiresUnreferencedCodeMethod ()
			{
			}
		}

		static void MethodWithGenericWhichRequiresMethods<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicMethods)] T> ()
		{
		}

		class TypeWithGenericWhichRequiresNonPublicFields<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.NonPublicFields)] T> { }

		class TestType { }
	}
}
