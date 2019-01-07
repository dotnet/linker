using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.LinkXml {
	public class PreservesOverriddenEvent {
		public static void Main ()
		{
		}

		[Kept]
		abstract class Base {
			[Kept]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public abstract event EventHandler Foo;
		}

		[Kept]
		// We have to keep the base type in this case because the link xml requested that all methods be kept.
		// Removing the base type with an override kept would produce invalid IL. 
		[KeptBaseType (typeof (Base))]
		class Bar : Base {
			[Kept]
			[KeptBackingField]
			[KeptEventAddMethod]
			[KeptEventRemoveMethod]
			public override event EventHandler Foo;
		}
	}
}