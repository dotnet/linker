using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass4 {
		public static void Main ()
		{
			new A ().Foo ();
		}

		interface IFoo {
			[Removed]
			void Foo ();
		}

		class B {
			[Kept]
			public void Foo ()
			{
			}
		}

		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
