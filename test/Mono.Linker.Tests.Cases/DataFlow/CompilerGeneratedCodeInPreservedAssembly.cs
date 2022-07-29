// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	// This test tries to hit a case where the entire assemly is preserved (via descriptor, NOT action)
	// meaning we will go and mark all types and members in it.
	// At the same time there's a compiler generated method (local function) which is called from
	// a branch which will be removed due to constant propagation.
	// And also the test is structured such that the caller of the local function is first seen
	// by the compiler generated code caches after its body has been modified.
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[SetupLinkerArgument ("--enable-opt", "ipconstprop")]
	[SetupLinkerDescriptorFile ("CompilerGeneratedCodeInPreservedAssembly.xml")]
	class CompilerGeneratedCodeInPreservedAssembly
	{
		[ExpectedWarning ("IL2026", "--" + nameof (Inner) + "." + nameof (Inner.WithWarningLocalFunctionInner) + "--")]
		[ExpectedWarning ("IL2026", "--" + nameof (WithWarningLocalFunction) + "--")]
		public static void Main ()
		{
			Inner.WithLocalFunctionInner ();
			WithLocalFunction ();
			Inner.WithWarningLocalFunctionInner ();
			WithWarningLocalFunction ();
		}

		class Inner
		{
			// In this case the compiler generated state will see the modified body
			// and thus will see the local function as orphaned.
			// The method will be marked (due to the descriptor), but its data flow processing
			// will be skipped (it's compiler generated).
			// The user method will also not be scanned for data flow since it won't
			// see the local function as its child.
			public static void WithLocalFunctionInner ()
			{
				if (AlwaysFalse) {
					LocalWithWarning ();
				}

				// Analyzer doesn't implement constant propagation and branch removal, so it reaches this code
				[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Analyzer)]
				void LocalWithWarning ()
				{
					// No warning
					Requires ();
				}
			}

			// Same as above, but with a pattern in the local function that warns even if we
			// don't do data flow analysis. The warning is not suppressed by RUC, because the local
			// function's association with the method is not detected.
			[RequiresUnreferencedCode ("--" + nameof (Inner) + "." + nameof (WithWarningLocalFunctionInner) + "--")]
			public static void WithWarningLocalFunctionInner ()
			{
				if (AlwaysFalse) {
					LocalWithWarning<int> ();
				}

				[ExpectedWarning ("IL2091", ProducedBy = ProducedBy.Trimmer)]
				void LocalWithWarning<T> ()
				{
					// Warning!
					RequiresAllOnT<T> ();
				}
			}
		}

		// In this case the compiler generated state will currently see the original method body
		// so if will treat LocalWithWarning local function as a "callee" of the user method.
		// This will trigger data flow analysis for the WithLocalFunction, but no warning is produced
		// because the local function is never reached during the interprocedural scan.
		public static void WithLocalFunction ()
		{
			if (AlwaysFalse) {
				LocalWithWarning ();
			}

			// Analyzer doesn't implement constant propagation and branch removal, so it reaches this code
			[ExpectedWarning ("IL2026", ProducedBy = ProducedBy.Analyzer)]
			void LocalWithWarning ()
			{
				Requires ();
			}
		}

		// Same as above, but with a pattern in the local function that warns even if we
		// don't do data flow analysis. The warning is suppressed by RUC because the local
		// function is associated with the method.
		[RequiresUnreferencedCode ("--" + nameof (WithWarningLocalFunction) + "--")]
		public static void WithWarningLocalFunction ()
		{
			if (AlwaysFalse) {
				LocalWithWarning<int> ();
			}

			void LocalWithWarning<T> ()
			{
				// No warning
				RequiresAllOnT<T> ();
			}
		}
		public static bool AlwaysFalse => false;

		[RequiresUnreferencedCode ("RUC")]
		static void Requires () { }

		static void RequiresAllOnT<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] T> () { }
	}
}
