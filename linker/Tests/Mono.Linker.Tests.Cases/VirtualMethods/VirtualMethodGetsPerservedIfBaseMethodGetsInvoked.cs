using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class VirtualMethodGetsPerservedIfBaseMethodGetsInvoked {
		public static void Main ()
		{
			new A ();
			new B ().Foo ();
		}

		class B {
			[Kept]
			public virtual void Foo ()
			{
			}
		}

		class A : B {
			[Kept]
			public override void Foo ()
			{
			}
		}
	}
}
