using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class ReturnTypeDownCastedToObjectAndPassedToMethod {
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
		static void UseAsBase (object o)
		{
		}

		[Kept]
		class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))] // Technically we could change this to Object, however, it would be hard to detect when it is safe to
		class Derived : Base {
		}
	}
}