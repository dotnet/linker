// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class ExponentialDataFlow
	{
		public static void Main ()
		{
			ExponentialArrayInStateMachine.Test ();
		}

		class ExponentialArrayInStateMachine
		{
			// Force state machine
			static async Task RecursiveReassignment ()
			{
				typeof (TestType).RequiresAll (); // Force data flow analysis

				object[] args = null;
				args = new[] { args };
			}

			public static void Test ()
			{
				RecursiveReassignment ().Wait ();
			}
		}

		class TestType { }
	}
}