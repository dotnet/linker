using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class ReturnTypeStoredInField {
		[Kept]
		private static Base field;
		
		public static void Main ()
		{
			field = GetValue ();
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