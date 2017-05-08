﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass6 {
		public static void Main ()
		{
			B tmp = new B ();
			IFoo i = new C ();
			i.Foo ();
		}

		interface IFoo {
			[Kept]
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B {
			[Kept] // FIXME: Needs to be removed
			public void Foo ()
			{
			}
		}

		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}

		[KeptMember (".ctor()")]
		[KeptInterface (typeof (IFoo))]
		class C : IFoo {
			[Kept]
			public void Foo ()
			{
			}
		}
	}
}
