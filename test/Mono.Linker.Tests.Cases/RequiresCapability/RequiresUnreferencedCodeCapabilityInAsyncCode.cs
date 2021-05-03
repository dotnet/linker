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

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class RequiresUnreferencedCodeCapabilityInAsyncCode
	{
		[UnconditionalSuppressMessage ("IL2026", "")]
		public static void Main ()
		{
			TestBeforeAwait ();
			TestAfterAwait ();
		}

		[RequiresUnreferencedCode ("--TestBeforeAwait--")]
		static async void TestBeforeAwait ()
		{
			MethodRequiresUnreferencedCode ();
			await AsyncMethod ();
		}

		[RequiresUnreferencedCode ("--TestAfterAwait--")]
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