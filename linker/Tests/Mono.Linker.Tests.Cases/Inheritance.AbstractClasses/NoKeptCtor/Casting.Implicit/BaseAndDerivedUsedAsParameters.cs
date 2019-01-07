using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Implicit {
	public class BaseAndDerivedUsedAsParameters {
		public static void Main ()
		{
			Method (null);
			Method2 (null);
		}

		[Kept]
		static void Method (Base b)
		{
		}

		[Kept]
		static void Method2 (Derived d)
		{
		}

		[Kept]
		class Base {
		}

		[Kept]
		class Derived : Base {
		}
	}
}