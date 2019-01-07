using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Generics.Constraints {
	public class PublicTypeAsGenericConstraintOnMethod {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class Base {
		}

		[Kept]
		abstract class Constraint {
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper<DerivedFromConstraint> ();
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
			[KeptBaseType (typeof (Constraint))]
			class DerivedFromConstraint : Constraint {
			}
		}
	}
}