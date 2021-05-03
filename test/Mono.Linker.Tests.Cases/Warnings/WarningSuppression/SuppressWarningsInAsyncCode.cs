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

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class SuppressWarningsInAsyncCode
	{
		public static void Main ()
		{
			TestBeforeAwait ();
			TestAfterAwait ();
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static async void TestBeforeAwait ()
		{
			MethodRequiresUnreferencedCode ();
			await AsyncMethod ();
		}

		[UnconditionalSuppressMessage ("IL2026", "")]
		static async void TestAfterAwait ()
		{
			await AsyncMethod ();
			MethodRequiresUnreferencedCode ();
		}

		static async Task<int> AsyncMethod ()
		{
			return await Task.FromResult (0);
		}

		[RequiresUnreferencedCode ("--MethodRequiresUnreferencedCode--")]
		static void MethodRequiresUnreferencedCode () { }
	}
}
