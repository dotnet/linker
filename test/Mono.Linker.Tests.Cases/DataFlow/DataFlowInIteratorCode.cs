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
	public class DataFlowInIteratorCode
	{
		public static void Main ()
		{
			TestParameter (typeof (TestType));
			TestLocalVariable ();
			TestGenericParameter<TestType> ();
		}

		static IEnumerable<int> TestParameter ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] Type type)
		{
			type.GetMethod ("BeforeIteratorMethod");
			yield return 1;
			type.GetMethod ("AfterIteratorMethod");
		}

		static IEnumerable<int> TestLocalVariable ()
		{
			Type type = typeof (TestType);
			type.GetMethod ("BeforeIteratorMethod");
			yield return 1;
			type.GetMethod ("AfterIteratorMethod");
		}

		static IEnumerable<int> TestGenericParameter<[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)] T> ()
		{
			typeof (T).GetMethod ("BeforeIteratorMethod");
			yield return 1;
			typeof (T).GetMethod ("AfterIteratorMethod");
		}

		class TestType
		{
		}
	}
}
