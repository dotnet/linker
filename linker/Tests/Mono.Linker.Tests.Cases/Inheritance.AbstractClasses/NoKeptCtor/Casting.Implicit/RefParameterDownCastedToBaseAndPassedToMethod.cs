using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class RefParameterDownCastedToBaseAndPassedToMethod {
		public static void Main ()
		{
			Derived d = null;
			GetValue (ref d);
			UseAsBase (d);
		}

		[Kept]
		static void GetValue (ref Derived d)
		{
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