using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods {
	class ClassImplemtingInterfaceMethodsThroughBaseClass4 {
		public static void Main ()
		{
			new A ().Foo ();
		}

		[Kept]
		interface IFoo {
			void Foo ();
		}

		[KeptMember (".ctor()")]
		class B {
			[Kept]
			public void Foo ()
			{
			}
		}

		[KeptMember (".ctor()")]
		[KeptBaseType ("Mono.Linker.Tests.Cases.VirtualMethods.ClassImplemtingInterfaceMethodsThroughBaseClass4/B")]
		[KeptInterface ("Mono.Linker.Tests.Cases.VirtualMethods.ClassImplemtingInterfaceMethodsThroughBaseClass4/IFoo")] // FIXME: Why is it not removed
		class A : B, IFoo {
			//my IFoo.Foo() is actually implemented by B which doesn't know about it.
		}
	}
}
