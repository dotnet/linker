using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	[IgnoreTestCase ("We would like this to be true, but it is not yet today")]
	class VirtualMethodGetsStrippedIfImplementingMethodGetsInvokedDirectly {
		public static void Main ()
		{
			new A ().Foo ();
		}

		class B {
			[Removed]
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
