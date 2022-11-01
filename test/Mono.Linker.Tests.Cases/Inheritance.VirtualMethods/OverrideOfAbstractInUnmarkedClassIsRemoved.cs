// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.VirtualMethods
{
	public class OverrideInUnmarkedClassIsRemoved
	{
		[Kept]
		public static void Main ()
		{
			MarkedBase x = new MarkedDerived ();
			x.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class MarkedBase
		{
			[Kept]
			public abstract int Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MarkedBase))]
		class MarkedDerived : MarkedBase
		{
			[Kept]
			public override int Method () => 1;
		}

		class UnmarkedDerived : MarkedBase
		{
			public override int Method () => 1;
		}
	}
}
