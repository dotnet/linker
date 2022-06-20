// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[SetupCompileArgument ("/unsafe")]
	[ExpectedNoWarnings]
	public class CompilerGeneratedCodeDataflow
	{
		public static void Main ()
		{
			Iterator.Test ();
			Async.Test ();
			AsyncIterator.Test ();
			LocalFunction.Test ();
			Lambda.Test ();
			Complex.Test ();
		}

		class Iterator
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
				CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowAcrossYieldReturn ()
			{
				Type t = GetWithPublicMethods ();
				yield return 0;
				t.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			// Linker doesn't track backwards branches
			// [ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true)]
			static IEnumerable<int> FlowAcrossYieldReturnWithBackwardsBranch (int n = 0)
			{
				Type t = GetWithPublicMethods ();
				for (int i = 0; i < n; i++) {
					yield return 0;
					t.RequiresAll ();
					yield return 1;
					t = GetWithPublicFields ();
				}
			}

			public static void Test ()
			{
				FlowAcrossYieldReturn ();
				FlowAcrossYieldReturnWithBackwardsBranch ();
			}
		}

		class Async
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
				CompilerGeneratedCode = true)]
			static async void FlowAcrossAwait ()
			{
				Type t = GetWithPublicMethods ();
				await MethodAsync ();
				t.RequiresAll ();
			}

			public static void Test ()
			{
				FlowAcrossAwait ();
			}
		}

		class AsyncIterator
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
				ProducedBy = ProducedBy.Trimmer)]
			static async IAsyncEnumerable<int> FlowAcrossAwaitAndYieldReturn ()
			{
				Type t = GetWithPublicMethods ();
				await MethodAsync ();
				yield return 0;
				t.RequiresAll ();
			}

			public static void Test ()
			{
				FlowAcrossAwaitAndYieldReturn ();
			}
		}

		class LocalFunction
		{
			static void WarningsInBody ()
			{
				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				static void LocalFunction ()
				{
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				}

				LocalFunction ();
			}

			static void WarningsInBodyUnused ()
			{
				// Trimmer doesn't warn because this is unused code.
				static void LocalFunction ()
				{
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				}
			}

			static void ReadCapturedVariable ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction ()
				{
					t.RequiresAll ();
				}
			}

			static void ReadMergedCapturedVariable (bool b = false)
			{
				Type t;
				if (b) {
					t = GetWithPublicMethods ();
				} else {
					t = GetWithPublicFields ();
				}

				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInMultipleBranches (bool b = false)
			{
				Type t;
				if (b) {
					t = GetWithPublicMethods ();
					LocalFunction ();
				} else {
					t = GetWithPublicFields ();
					LocalFunction ();
				}

				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableWithBackwardsBranch (int i = 0)
			{
				Type t = GetWithPublicMethods ();
				while (true) {
					LocalFunction ();
					if (i++ == 5)
						break;
					t = GetWithPublicFields ();
				}

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				// Linker doesn't handle backwards branches
				// [ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll))]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInMultipleFunctions ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				CallerOfLocalFunction ();

				void CallerOfLocalFunction ()
				{
					t = GetWithPublicFields ();
					LocalFunction ();
				}

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			static void ReadCapturedVariableInCallGraphCycle ()
			{
				Type t = GetUnknownType ();
				A ();

				void A ()
				{
					t = GetWithPublicMethods ();
					LocalFunction ();
					B ();
				}

				void B ()
				{
					t = GetWithPublicFields ();
					LocalFunction ();
					A ();
				}

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			public static void ReadCapturedParameter (Type tParameter)
			{
				LocalFunction ();

				[ExpectedWarning ("IL2067", nameof (ReadCapturedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => tParameter.RequiresAll ();
			}

			public static void ReadCapturedParameterAfterWrite (Type tParameter)
			{
				tParameter = GetWithPublicMethods (); // Changes the state of the parameter (only allowed for hoisted locals)
				LocalFunction ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => tParameter.RequiresAll ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
				// analyzer clears all local state, but trimmer doesn't
				ProducedBy = ProducedBy.Trimmer)]
			static void ReadCapturedVariableWithUnhoistedLocals ()
			{
				Type t = GetWithPublicMethods ();
				Type notCaptured = GetWithPublicFields ();
				LocalFunction ();
				notCaptured.RequiresAll ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			static void WriteCapturedVariable ()
			{
				Type t = GetAll ();
				LocalFunction ();
				// No warning. This is an analysis hole!
				// We don't flow state out of local functions.
				t.RequiresAll ();

				void LocalFunction () => t = GetWithPublicMethods ();
			}

			public static void Test ()
			{
				WarningsInBody ();
				WarningsInBodyUnused ();
				ReadCapturedVariable ();
				ReadMergedCapturedVariable ();
				ReadCapturedVariableInMultipleBranches ();
				ReadCapturedVariableInMultipleFunctions ();
				ReadCapturedVariableInCallGraphCycle ();
				ReadCapturedVariableWithBackwardsBranch ();
				ReadCapturedParameter (null);
				ReadCapturedParameterAfterWrite (null);
				ReadCapturedVariableWithUnhoistedLocals ();
				WriteCapturedVariable ();
			}
		}

		class Lambda
		{
			static void WarningsInBody ()
			{
				var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				};

				lambda ();
			}

			static void WarningsInBodyUnused ()
			{
				var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => {
					Type t = GetWithPublicMethods ();
					t.RequiresAll ();
				};
			}

			static void ReadCapturedVariable ()
			{
				Type t = GetWithPublicMethods ();
				Action lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => t.RequiresAll ();
				lambda ();
			}

			static void ReadCapturedVariableAfterWriteAfterDefinition ()
			{
				Type t = GetWithPublicFields ();

				Action lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)] () => t.RequiresAll ();

				t = GetWithPublicMethods ();
				// No warning. This is an analysis hole!
				// We don't track captured delegates. Instead, we analyze lambdas
				// at the point of declaration.
				lambda ();
			}

			public static void ReadCapturedParameter (Type tParameter)
			{
				var lambda =
					[ExpectedWarning ("IL2067", nameof (ReadCapturedParameter), "tParameter", nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => tParameter.RequiresAll ();

				lambda ();
			}

			public static void ReadCapturedParameterAfterWrite (Type tParameter)
			{
				tParameter = GetWithPublicMethods ();
				var lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => tParameter.RequiresAll ();
				lambda ();
			}

			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
				ProducedBy = ProducedBy.Trimmer)]
			static void ReadCapturedVariableWithUnhoistedLocals ()
			{
				Type t = GetWithPublicMethods ();
				Type notCaptured = GetWithPublicFields ();
				Action lambda =
					[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				() => t.RequiresAll ();
				lambda ();
				notCaptured.RequiresAll ();
			}

			static void WriteCapturedVariable ()
			{
				Type t = GetAll ();
				Action lambda = () => t = GetWithPublicMethods ();
				lambda ();
				// No warning. This is an analysis hole!
				// We don't flow state out of lambdas.
				t.RequiresAll ();
			}

			public static void Test ()
			{
				WarningsInBody ();
				WarningsInBodyUnused ();
				ReadCapturedVariable ();
				ReadCapturedVariableAfterWriteAfterDefinition ();
				ReadCapturedParameter (null);
				ReadCapturedParameterAfterWrite (null);
				ReadCapturedVariableWithUnhoistedLocals ();
				WriteCapturedVariable ();
			}
		}

		class Complex
		{
			[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)]
			// Linker merges branches going forward
			[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll), CompilerGeneratedCode = true,
					ProducedBy = ProducedBy.Trimmer)]
			static IEnumerable<int> IteratorWithLocalFunctions ()
			{
				Type t = GetWithPublicMethods ();
				LocalFunction ();

				yield return 0;

				LocalFunction ();
				t = GetWithPublicFields ();
				LocalFunction ();
				t.RequiresAll ();

				[ExpectedWarning ("IL2072", nameof (GetWithPublicMethods), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				[ExpectedWarning ("IL2072", nameof (GetWithPublicFields), nameof (DataFlowTypeExtensions.RequiresAll),
					ProducedBy = ProducedBy.Trimmer)]
				void LocalFunction () => t.RequiresAll ();
			}

			public static void Test ()
			{
				IteratorWithLocalFunctions ();
			}
		}

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)]
		static Type GetAll () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
		static Type GetWithPublicMethods () => null;

		[return: DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
		static Type GetWithPublicFields () => null;

		static Type GetUnknownType () => null;

		static async Task<int> MethodAsync ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("RUC")]
		static void RUCMethod () { }

		struct TestStruct
		{
			[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicFields)]
			public Type TypeWithPublicFields => null;
		}
	}
}