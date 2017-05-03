﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class UninvokedInterfaceMemberGetsRemoved {
		public static void Main ()
		{
			new B ();
		}

		[Kept]
		interface I {
			void Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptInterface ("Mono.Linker.Tests.Cases.Basic.UninvokedInterfaceMemberGetsRemoved/I")]
		class B : I {
			public void Method ()
			{
			}
		}
	}
}