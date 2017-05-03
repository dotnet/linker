using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass5 {
		public static void Main ()
		{
			new A ();
		}

		interface IFoo {
			[Removed]
			void Foo ();
		}

		class B {
			[Removed]
			public void Foo ()
			{
			}
		}

		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
