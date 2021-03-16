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
using Mono.Linker.Tests.Cases.Expectations.Helpers;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SkipKeptItemsValidation]
	public class TypeBaseType
	{
		public static void Main()
		{
			TestAllPropagated (null);
		}

		[RecognizedReflectionAccessPattern]
		static void TestAllPropagated ([DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.All)] Type derivedType)
		{
			derivedType.BaseType.RequiresAll ();
		}
	}
}
