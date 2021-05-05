// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[SetupCompileArgument ("/debug:full")] // This will produce symbols and thus warnings will contain source info
	public class WarningLocationInCompilerGeneratedCode
	{
		public static void Main ()
		{
			TestInLambda ();
			TestInLocalFunction ();
			TestIterator ();
			TestAsync ();
		}

		// The warning is generated but with a "wrong" location
		// Mono.Linker.Tests.Cases.Warnings.WarningLocationInCompilerGeneratedCode.<>c.<TestInLambda>b__1_0()
		// So the test infra doesn't match it.
		[ExpectedWarning ("IL2026")]
		static void TestInLambda ()
		{
			Action a = () => MethodRequiresUnreferencedCode ();
		}

		[ExpectedWarning ("IL2026")]
		static void TestInLocalFunction ()
		{
			LocalFunction ();

			void LocalFunction ()
			{
				MethodRequiresUnreferencedCode ();
			}
		}

		[ExpectedWarning ("IL2026")]
		static IEnumerable<int> TestIterator ()
		{
			MethodRequiresUnreferencedCode ();
			yield return 1;
		}

		[ExpectedWarning ("IL2026")]
		static async void TestAsync ()
		{
			MethodRequiresUnreferencedCode ();
			await AsyncMethod ();
		}

		[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
		static void MethodRequiresUnreferencedCode (int p = 0) { }

		static async Task<int> AsyncMethod ()
		{
			return await Task.FromResult (0);
		}
	}
}
