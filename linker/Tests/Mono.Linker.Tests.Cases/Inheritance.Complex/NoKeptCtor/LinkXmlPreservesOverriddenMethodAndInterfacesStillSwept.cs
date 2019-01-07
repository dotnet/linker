using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Complex.NoKeptCtor {
	public class LinkXmlPreservesOverriddenMethodAndInterfacesStillSwept {
		public static void Main ()
		{
		}

		[Kept]
		abstract class Base {
			[Kept]
			public abstract void Foo ();
		}

		interface IOne {
			void One ();
		}
		
		interface ITwo {
			void Two ();
		}

		[Kept]
		// We have to keep the base type in this case because the link xml requested that all methods be kept.
		// Removing the base type with an override kept would produce invalid IL. 
		[KeptBaseType (typeof (Base))]
		class Bar : Base, IOne, ITwo {
			[Kept]
			public override void Foo ()
			{
			}

			public void One ()
			{
			}

			public void Two ()
			{
			}
		}
	}
}