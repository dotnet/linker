using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class ReturnTypeStoredInLocal {
		public static void Main ()
		{
			Base local = GetValue ();
		}

		[Kept]
		static Derived GetValue ()
		{
			return null;
		}

		[Kept]
		class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Derived : Base {
		}
	}
}