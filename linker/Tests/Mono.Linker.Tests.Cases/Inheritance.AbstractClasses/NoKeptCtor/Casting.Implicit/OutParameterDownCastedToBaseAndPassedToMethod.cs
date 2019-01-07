using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class OutParameterDownCastedToBaseAndPassedToMethod {
		public static void Main ()
		{
			Derived d;
			GetValue (out d);
			UseAsBase (d);
		}

		[Kept]
		static void GetValue (out Derived d)
		{
			d = null;
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