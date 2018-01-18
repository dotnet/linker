﻿using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class UsedEventOnInterfaceIsRemovedWhenUsedFromClass {
		static void Main ()
		{
			var bar = new Bar ();
			var jar = new Jar ();

			bar.Ping += Bar_Ping;
		}

		[Kept]
		private static void Bar_Ping (object sender, EventArgs e)
		{
		}

		[Kept]
		interface IFoo {
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Bar : IFoo {
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public event EventHandler Ping;
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class Jar : IFoo {
		}
	}
}
