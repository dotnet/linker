using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Complex.NoKeptCtor {
	public class LinkXmlPreservesOverriddenMethodAlsoForInterface {
		public static void Main ()
		{
		}

		[Kept]
		abstract class Base {
			[Kept]
			public abstract void Foo ();
		}

		interface IFoo {
			void Foo ();
		}

		[Kept]
		// We have to keep the base type in this case because the link xml requested that all methods be kept.
		// Removing the base type with an override kept would produce invalid IL. 
		[KeptBaseType (typeof (Base))]
		class Bar : Base, IFoo {
			[Kept]
			public override void Foo ()
			{
			}
		}
	}
}