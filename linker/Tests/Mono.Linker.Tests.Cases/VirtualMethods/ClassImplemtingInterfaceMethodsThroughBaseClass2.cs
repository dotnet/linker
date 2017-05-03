using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	[IgnoreTestCase ("This test fails and is ignored for an unknown reason. We should investigate this more.")]
	class ClassImplemtingInterfaceMethodsThroughBaseClass2 {
		public static void Main ()
		{
			new B ();
			IFoo i = null;
			i.Foo ();
		}

		interface IFoo {
			[Kept]
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
