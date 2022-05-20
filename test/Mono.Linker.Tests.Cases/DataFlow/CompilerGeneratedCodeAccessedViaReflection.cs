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
			StateMachines.Test ();
			Lambdas.Test ();
			LocalFunctions.Test ();
		}

		class StateMachines
		{
			public static IEnumerable<int> IteratorWithoutDataflow ()
			{
				yield return 0;
			}

			[ExpectedWarning ("IL2026", "--MethodWithRequires--", CompilerGeneratedCode = true)]
			[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
			public static IEnumerable<int> IteratorCallsMethodWithRequires ()
			{
				yield return 0;
				MethodWithRequires ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorWithAnnotatedDataflow ()
			{
				var t = GetAll ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static IEnumerable<int> IteratorWithUnannotatedDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return 0;
				t.RequiresAll ();
			}

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

			[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			public static async IAsyncEnumerable<int> AsyncIteratorWithUnannotatedDataflow ()
			{
				var t = GetWithPublicMethods ();
				yield return await MethodAsync ();
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2118", nameof (IteratorWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (IteratorCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (IteratorWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncIteratorWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncIteratorCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (AsyncIteratorWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (StateMachines).RequiresAll ();
			}
		}

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
					() => MethodWithRequires ();
				lambda();
			}

			static void LambdaWithAnnotatedDataflow ()
			{
				var lambda =
				() => {
					var t = GetAll ();
					t.RequiresAll ();
				};
				lambda();
			}

			static void LambdaWithUnannotatedDataflow ()
			{
				var lambda =
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

			[ExpectedWarning ("IL2118", nameof (LambdaCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LambdaWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LambdaWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LambdaWithCapturedTypeToDAM), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (Lambdas).RequiresAll ();
			}
		}

		class LocalFunctions
		{
			static void LocalFunctionWithoutDataflow ()
			{
				int LocalFunction () => 0;
				LocalFunction();
			}

			static void LocalFunctionCallsMethodWithRequires ()
			{
				[ExpectedWarning ("IL2026", "--MethodWithRequires--")]
				[ExpectedWarning ("IL3002", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				[ExpectedWarning ("IL3050", "--MethodWithRequires--", ProducedBy = ProducedBy.Analyzer)]
				void LocalFunction() => MethodWithRequires ();
				LocalFunction ();
			}

			static void LocalFunctionWithAnnotatedDataflow ()
			{
				void LocalFunction() {
					var t = GetAll ();
					t.RequiresAll ();
				};
				LocalFunction ();
			}

			static void LocalFunctionWithUnannotatedDataflow ()
			{
				[ExpectedWarning ("IL2072", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () {
					var t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
				LocalFunction();
			}

			static void LocalFunctionWithCapturedTypeToDAM ()
			{
				var t = GetAll ();
				[ExpectedWarning ("IL2077", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () {
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

			[ExpectedWarning ("IL2118", nameof (LocalFunctionCallsMethodWithRequires), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithAnnotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithUnannotatedDataflow), ProducedBy = ProducedBy.Trimmer)]
			[ExpectedWarning ("IL2118", nameof (LocalFunctionWithCapturedTypeToDAM), ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				typeof (LocalFunctions).RequiresAll ();
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
