using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticMethodUsingNestedProtectedTypeFromBaseInBody4 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			[KeptBaseType (typeof (Constraint))]
			protected class NestedType : Constraint {
				public void Foo ()
				{
				}
			}
		}

		[Kept]
		abstract class Constraint {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				if (GetAValue () is Constraint)
				{
					Helper ();
				}
			}

			[Kept]
			static void Helper ()
			{
			}

			[Kept]
			static DerivedFromNested GetAValue ()
			{
				return null;
			}
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested : NestedType {
			}
		}
	}
}