using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Generics.Constraints {
	public class PublicTypeAsGenericConstraintOnMethod2 {
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
				Helper<DerivedFromNested> ();
			}

			[Kept]
			private static Container<T> Helper<T> () where T : Constraint
			{
				return null;
			}
			
			[Kept]
			class Container<T> {
			}

			[Kept]
			// Technically we could probably change this to `Constraint` but for now
			// we are implementing this as all or nothing to keep things simple
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested : NestedType {
			}
		}
	}
}