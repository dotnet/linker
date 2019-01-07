using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.LinkXml {
	public class PreservesOverriddenProperty {
		public static void Main ()
		{
		}

		[Kept]
		abstract class Base {
			[Kept]
			public abstract int Foo { [Kept] get; [Kept] set; }
		}

		[Kept]
		// We have to keep the base type in this case because the link xml requested that all methods be kept.
		// Removing the base type with an override kept would produce invalid IL. 
		[KeptBaseType (typeof (Base))]
		class Bar : Base {
			[Kept]
			[KeptBackingField]
			public override int Foo { [Kept] get; [Kept] set; }
		}
	}
}