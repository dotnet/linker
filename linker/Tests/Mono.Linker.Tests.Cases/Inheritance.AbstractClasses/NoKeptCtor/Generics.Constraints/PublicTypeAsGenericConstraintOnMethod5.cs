using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Generics.Constraints {
	public class PublicTypeAsGenericConstraintOnMethod5 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class Base {
		}

		abstract class Constraint {
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper3<NestedContainer>();
			}

			[Kept]
			private static T Helper3<T> () where T : Container<Container<DerivedFromConstraint>>
			{
				return null;
			}

			[Kept]
			class Container<T> {
			}

			[Kept]
			[KeptBaseType(typeof(Container<Container<DerivedFromConstraint>>))]
			class NestedContainer : Container<Container<DerivedFromConstraint>> {
			}

			[Kept]
			class DerivedFromConstraint : Constraint {
			}
		}
	}
}