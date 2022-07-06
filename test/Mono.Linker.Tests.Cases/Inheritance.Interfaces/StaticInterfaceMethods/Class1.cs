// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	public class UnusedStaticInterfaceMethods
	{
		[Kept]
		public void Main ()
		{
			Foo.KeepFoo ();
			KeepIFooStaticUnused (null);
			((IFooStaticUsed)null).GetIntUsed ();
			IFooStaticUsed.GetIntStaticUsed ();
		}

		[Kept]
		interface IFooStaticUnused
		{
			int GetInt () => 0;
			static abstract int GetStaticInt ();
		}

		[Kept]
		interface IFooStaticUsed
		{
			[Kept]
			int GetIntUsed () => 0;
			[Kept]
			static virtual int GetIntStaticUsed () => 1;
		}

		void KeepIFooStaticUnused (IFooStaticUnused x) { }

		[Kept]
		[KeptInterface(typeof(IFooStaticUsed))]
		class Foo : IFooStaticUnused, IFooStaticUsed
		{
			public int GetInt () => 1;
			public static int GetStaticInt () => 1;
			public static void KeepFoo () { }
			public int GetIntUsed () => 1;
			public static int GetIntStaticUsed () => 0;
		}
	}
}
