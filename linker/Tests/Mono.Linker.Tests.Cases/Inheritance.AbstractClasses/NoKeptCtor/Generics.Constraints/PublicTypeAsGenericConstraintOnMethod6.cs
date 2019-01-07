using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Generics.Constraints {
	public class PublicTypeAsGenericConstraintOnMethod6 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class Base {
		}

		[Kept]
		abstract class Constraint {
		}

		[Kept]  // We could remove this since there is no constraint when this type is used
		abstract class Constraint2 {
		}

		[Kept]  // We could remove this since there is no constraint when this type is used
		abstract class Constraint3 {
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper2<DerivedFromConstraint, DerivedFromConstraint2, DerivedFromConstraint3>(null, null);
			}

			[Kept]
			private static Container<T1> Helper2<T1, T2, T3> (T2 arg1, Container<T3> arg2)
				where T1 : Constraint
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

			[Kept]
			[KeptBaseType (typeof (Constraint2))] // We could remove this since there is no constraint when this type is used
			class DerivedFromConstraint2 : Constraint2 {
			}

			[Kept]
			[KeptBaseType (typeof (Constraint3))]  // We could remove this since there is no constraint when this type is used
			class DerivedFromConstraint3 : Constraint3 {
			}
		}
	}
}