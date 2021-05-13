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

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	public class DataFlowInAsyncCode
	{
		public static void Main ()
		{
			TestParameter (typeof (TestClass));
			TestLocalVariable ();
			TestGenericParameter<TestClass> ();
			TestWarning<TestClass> ();
		}

		static async void TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			type.GetMethod ("BeforeAsyncMethod");
			await AsyncMethod ();
			type.GetMethod ("AfterAsyncMethod");
		}

		static async void TestLocalVariable ()
		{
			Type type = typeof (TestClass);
			type.GetMethod ("BeforeAsyncMethod");
			await AsyncMethod ();
			type.GetMethod ("AfterAsyncMethod");
		}

		static async void TestGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).GetMethod ("BeforeIteratorMethod");
			await AsyncMethod ();
			typeof (T).GetMethod ("AfterIteratorMethod");
		}

		[ExpectedWarning("IL2000", "'TInput'")]
		static async void TestWarning<TInput>()
		{
			typeof (TInput).GetMethod ("InAsyncMethod");
			await AsyncMethod ();
		}

		static async Task<int> AsyncMethod ()
		{
			return await Task.FromResult (0);
		}

		class TestClass
		{
		}
	}
}
