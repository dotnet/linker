using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class LocalPassedToMethodAsBase2 {
		public static void Main ()
		{
			var dType = typeof (Derived).ToString();
			Base2 b = null;
			UseAsBase (b);
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

		class Base3 : Base2 {
		}

		[Kept]
		class Derived : Base3 {
		}
	}
}