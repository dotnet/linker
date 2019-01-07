using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class LocalPassedToMethodAsBase {
		public static void Main ()
		{
			Derived d = null;
			UseAsBase (d);
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
		class Base2 : Base {
		}
		
		[Kept]
		[KeptBaseType (typeof (Base2))]
		class Base3 : Base2 {
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		class Derived : Base3 {
		}
	}
}