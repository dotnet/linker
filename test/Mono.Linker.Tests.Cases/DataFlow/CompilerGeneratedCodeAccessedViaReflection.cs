// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.DataFlow;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	class CompilerGeneratedCodeAccessedViaReflection
	{
		public static void Main ()
		{
			IteratorStateMachines.Test ();
			AsyncStateMachines.Test ();
			AsyncIteratorStateMachines.Test ();
			Lambdas.Test ();
			LocalFunctions.Test ();
		}

		class BaseTypeWithIteratorStateMachines
		{
			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			public static IEnumerable<int> BaseIteratorWithAnnotatedDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}
		}

		[ExpectedWarning ("IL2120", "<" + nameof (BaseIteratorWithAnnotatedDataflow) + ">", "MoveNext()")]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class IteratorStateMachines : BaseTypeWithIteratorStateMachines
		{
			public static IEnumerable<int> IteratorWithoutDataflow ()
			{
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorCallsMethodWithRequires) + ">", CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorCallsMethodWithRequires ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithAnnotatedDataflow) + ">", CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorWithAnnotatedDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithUnannotatedDataflow) + ">", "MoveNext", CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorWithUnannotatedDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUCTypeWithIterators--")]
			class RUCTypeWithIterators
			{
				[ExpectedWarning ("IL2112", nameof (StaticIteratorCallsMethodWithRequires), "--RUCTypeWithIterators--")]
				[ExpectedWarning ("IL2112", "<" + nameof (StaticIteratorCallsMethodWithRequires) + ">", "--RUCTypeWithIterators--", CompilerGeneratedCode = true)] // state machine ctor
				public static IEnumerable<int> StaticIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}

				// BUG: this should also give IL2112 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
				// https://github.com/dotnet/linker/issues/2806
				// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
				// With that, the IL2119 warning should also go away.
				[ExpectedWarning ("IL2119", "<" + nameof (InstanceIteratorCallsMethodWithRequires) + ">", CompilerGeneratedCode = true)]
				public IEnumerable<int> InstanceIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithUnannotatedDataflow) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithAnnotatedDataflow) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithAnnotatedDataflow) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators) + "()", ProducedBy = ProducedBy.Trimmer)]
			// Expect to see warnings about RUC on type, for all static state machine members.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + "()", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + ">", ProducedBy = ProducedBy.Trimmer)]
			// BUG: this should also give IL2026 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
			// https://github.com/dotnet/linker/issues/2806
			// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
			// With that, the IL2118 warning should also go away.
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			public static void Test (IteratorStateMachines test = null)
			{
				typeof (IteratorStateMachines).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		class AsyncStateMachines
		{
			public static async Task AsyncWithoutDataflow ()
			{
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static async Task AsyncCallsMethodWithRequires ()
			{
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async Task AsyncWithAnnotatedDataflow ()
			{
				var t = GetAll ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async Task AsyncWithUnannotatedDataflow ()
			{
				var t = GetWithPublicMethods ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", nameof (AsyncWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (AsyncStateMachines).RequiresAll ();
			}
		}

		class AsyncIteratorStateMachines
		{
			public static async IAsyncEnumerable<int> AsyncIteratorWithoutDataflow ()
			{
				yield return await MethodAsync ();
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static async IAsyncEnumerable<int> AsyncIteratorCallsMethodWithRequires ()
			{
				yield return await MethodAsync ();
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async IAsyncEnumerable<int> AsyncIteratorWithAnnotatedDataflow ()
			{
				var t = GetAll ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll),
				nameof (AsyncIteratorWithUnannotatedDataflow), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async IAsyncEnumerable<int> AsyncIteratorWithUnannotatedDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", nameof (AsyncIteratorWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorCallsMethodWithRequires) + ">", "MoveNext", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncIteratorWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (AsyncIteratorStateMachines).RequiresAll ();
			}
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class Lambdas
		{
			static void LambdaWithoutDataflow ()
			{
				var lambda = () => 0;
				lambda ();
			}

			static void LambdaCallsMethodWithRequires ()
			{
				var lambda =
					[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaCallsMethodWithRequires) + ">")]
				() => MethodWithRequires ();
				lambda ();
			}

			// TODO: rename annotated dataflow... there are no annotations.
			static void LambdaWithAnnotatedDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithAnnotatedDataflow) + ">")]
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithAnnotatedParameter) + ">")]
			static void LambdaWithAnnotatedParameter ()
			{
				var lambda =
				[ExpectedWarning ("IL2114", "<" + nameof (LambdaWithAnnotatedParameter) + ">")]
				([DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.All)] Type t) => {
					t.RequiresAll ();
				};
				lambda (null);
			}

			static void LambdaWithUnannotatedDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithUnannotatedDataflow) + ">")]
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					var t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
				lambda ();
			}

			static void LambdaWithCapturedTypeToDAM ()
			{
				var t = GetWithPublicMethods ();
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">")]
				[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					t.RequiresAll ();
				};
				lambda ();
			}

			static void LambdaWithCapturedInt ()
			{
				int i = 0;
				var lambda =
				() => i;
				i++;
				lambda ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUCTypeWithLambdas--")]
			class RUCTypeWithLambdas
			{
				public void MethodWithLambdas ()
				{
					var lambda =
						[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">")]
					() => MethodWithRequires ();

					int i = 0;
					var lambdaWithCapturedState =
					[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">")]
					() => {
						i++;
						MethodWithRequires ();
					};

					lambda ();
					lambdaWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsMethodWithRequires) + ">", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithAnnotatedDataflow) + ">", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithAnnotatedParameter) + ">", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithUnannotatedDataflow) + ">", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">", ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLambdas) + "()", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">", ProducedBy = ProducedBy.Trimmer)]
			public static void Test (Lambdas test = null)
			{
				typeof (Lambdas).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class LocalFunctions
		{
			static void LocalFunctionWithoutDataflow ()
			{
				int LocalFunction () => 0;
				LocalFunction ();
			}

			static void LocalFunctionCallsMethodWithRequires ()
			{
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionCallsMethodWithRequires) + ">")]
				void LocalFunction () => MethodWithRequires ();
				LocalFunction ();
			}

			static void LocalFunctionWithAnnotatedDataflow ()
			{
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithAnnotatedDataflow) + ">")]
				void LocalFunction ()
				{
					var t = GetAll ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithUnannotatedDataflow ()
			{
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithUnannotatedDataflow) + ">")]
				void LocalFunction ()
				{
					var t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithCapturedTypeToDAM ()
			{
				var t = GetAll ();
				[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCapturedTypeToDAM) + ">")]
				void LocalFunction ()
				{
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithCapturedInt ()
			{
				int i = 0;
				int LocalFunction () => i;
				i++;
				LocalFunction ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithLocalFunctions) + "()", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUCTypeWithLocalFunctions--")]
			class RUCTypeWithLocalFunctions
			{
				public void MethodWithLocalFunctions ()
				{
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">")]
					void LocalFunction () => MethodWithRequires ();

					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">")]
					static void StaticLocalFunction () => MethodWithRequires ();

					int i = 0;
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">")]
					void LocalFunctionWithCapturedState ()
					{
						i++;
						MethodWithRequires ();
					}

					LocalFunction ();
					StaticLocalFunction ();
					LocalFunctionWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCapturedTypeToDAM), ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLocalFunctions) + "()", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunctionWithCapturedState", ProducedBy = ProducedBy.Trimmer)] // displayclass ctor
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "StaticLocalFunction", ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunction", ProducedBy = ProducedBy.Trimmer)]
			public static void Test (LocalFunctions test = null)
			{
				typeof (LocalFunctions).RequiresAll ();

				test.GetType ().RequiresAll ();
			}
		}

		[RequiresUnreferencedCode ("--MethodWithRequires--")]
		[RequiresAssemblyFiles ("--MethodWithRequires--")]
		[RequiresDynamicCode ("--MethodWithRequires--")]
		static void MethodWithRequires ()
		{
		}

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}


		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		static Type GetAll () => null;
	}
}
