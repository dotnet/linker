using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtor {
	public class UnusedTypeHasExplicitInterfacePropertyPreservedViaXml {
		public static void Main ()
		{
		}

		[Kept]
		interface IFoo {
			[Kept]
			int Foo { [Kept] get; [Kept] set; }
		}

		interface IBar {
			int Bar { get; set; }
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class A : IBar, IFoo {
			[Kept]
			int IFoo.Foo { [Kept] [ExpectBodyModified] get; [Kept] [ExpectBodyModified] set; }

			int IBar.Bar { get; set; }
		}
	}
}