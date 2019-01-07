using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class ReturnTypeDownCastedToBaseAndPassedToMethod {
		public static void Main ()
		{
			UseAsBase (GetValue ());
		}

		[Kept]
		static Derived GetValue ()
		{
			return null;
		}

		[Kept]
		static void UseAsBase (Base b)
		{
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