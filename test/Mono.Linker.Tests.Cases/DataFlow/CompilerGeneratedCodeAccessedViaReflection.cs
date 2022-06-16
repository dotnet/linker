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
			public static IEnumerable<int> BaseIteratorWithCorrectDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}
		}

		[ExpectedWarning ("IL2120", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "MoveNext",
			ProducedBy = ProducedBy.Trimmer)]
		[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		class IteratorStateMachines : BaseTypeWithIteratorStateMachines
		{
			public static IEnumerable<int> IteratorWithoutDataflow ()
			{
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--",
				ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--",
				ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL2119", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext", CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorCallsMethodWithRequires ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext", CompilerGeneratedCode = true)]
			public static IEnumerable<int> IteratorWithCorrectDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2119", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorWithProblematicDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2112", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--", CompilerGeneratedCode = true)]
			[RequiresUnreferencedCode ("--RUCTypeWithIterators--")]
			class RUCTypeWithIterators
			{
				[ExpectedWarning ("IL2112", nameof (StaticIteratorCallsMethodWithRequires), "--RUCTypeWithIterators--",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2112", "<" + nameof (StaticIteratorCallsMethodWithRequires) + ">", "--RUCTypeWithIterators--", CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)] // state machine ctor
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				public static IEnumerable<int> StaticIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}

				// BUG: this should also give IL2112 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
				// https://github.com/dotnet/linker/issues/2806
				// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
				// With that, the IL2119 warning should also go away.
				[ExpectedWarning ("IL2119", "<" + nameof (InstanceIteratorCallsMethodWithRequires) + ">", "MoveNext", CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				public IEnumerable<int> InstanceIteratorCallsMethodWithRequires ()
				{
					yield return 0;
					MethodWithRequires ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (IteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (BaseIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators) + "()", "--RUCTypeWithIterators--")]
			// Expect to see warnings about RUC on type, for all static state machine members.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + "()", "--RUCTypeWithIterators--")]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.StaticIteratorCallsMethodWithRequires) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			// BUG: this should also give IL2026 for the InstanceIteratorCallsMethodWithRequires state machine constructor.
			// https://github.com/dotnet/linker/issues/2806
			// [ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">")]
			// With that, the IL2118 warning should also go away.
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithIterators.InstanceIteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
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

			public static async Task AsyncWithCorrectDataflow ()
			{
				var t = GetAll ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async Task AsyncWithProblematicDataflow ()
			{
				var t = GetWithPublicMethods ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
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

			public static async IAsyncEnumerable<int> AsyncIteratorWithCorrectDataflow ()
			{
				var t = GetAll ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async IAsyncEnumerable<int> AsyncIteratorWithProblematicDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithProblematicDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorCallsMethodWithRequires) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (AsyncIteratorWithCorrectDataflow) + ">", "MoveNext",
				ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				() => MethodWithRequires ();
				lambda ();
			}

			static void LambdaWithCorrectDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCorrectDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda ();
			}

			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithCorrectParameter) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			static void LambdaWithCorrectParameter ()
			{
				var lambda =
				[ExpectedWarning ("IL2114", "<" + nameof (LambdaWithCorrectParameter) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				([DynamicallyAccessedMembersAttribute (DynamicallyAccessedMemberTypes.All)] Type t) => {
					t.RequiresAll ();
				};
				lambda (null);
			}

			static void LambdaWithProblematicDataflow ()
			{
				var lambda =
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithProblematicDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL2119", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
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
					[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					() => MethodWithRequires ();

					int i = 0;
					var lambdaWithCapturedState =
					[ExpectedWarning ("IL2119", "<" + nameof (MethodWithLambdas) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					() => {
						i++;
						MethodWithRequires ();
					};

					lambda ();
					lambdaWithCapturedState ();
				}
			}

			[ExpectedWarning ("IL2118", "<" + nameof (LambdaCallsMethodWithRequires) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCorrectDataflow) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2111", "<" + nameof (LambdaWithCorrectParameter) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithProblematicDataflow) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (LambdaWithCapturedTypeToDAM) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLambdas) + "()", "--RUCTypeWithLambdas--")]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", "<" + nameof (RUCTypeWithLambdas.MethodWithLambdas) + ">",
				ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionCallsMethodWithRequires) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => MethodWithRequires ();
				LocalFunction ();
			}

			static void LocalFunctionWithCorrectDataflow ()
			{
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCorrectDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction ()
				{
					var t = GetAll ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithProblematicDataflow ()
			{
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithProblematicDataflow) + ">",
					ProducedBy = ProducedBy.Trimmer)]
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
				[ExpectedWarning ("IL2119", "<" + nameof (LocalFunctionWithCapturedTypeToDAM) + ">",
					ProducedBy = ProducedBy.Trimmer)]
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
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					void LocalFunction () => MethodWithRequires ();

					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					static void StaticLocalFunction () => MethodWithRequires ();

					int i = 0;
					[ExpectedWarning ("IL2112", "<" + nameof (MethodWithLocalFunctions) + ">",
						ProducedBy = ProducedBy.Trimmer)]
					[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
					[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
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

			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsMethodWithRequires),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCorrectDataflow),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithProblematicDataflow),
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCapturedTypeToDAM),
				ProducedBy = ProducedBy.Trimmer)]
			// Expect RUC warnings for static, compiler-generated code warnings for instance.
			[ExpectedWarning ("IL2026", nameof (RUCTypeWithLocalFunctions) + "()", "--RUCTypeWithLocalFunctions--")]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunctionWithCapturedState",
				ProducedBy = ProducedBy.Trimmer)] // displayclass ctor
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "StaticLocalFunction",
				ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2026", "<" + nameof (RUCTypeWithLocalFunctions.MethodWithLocalFunctions) + ">", "LocalFunction",
				ProducedBy = ProducedBy.Trimmer)]
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
