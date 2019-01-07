using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Generics.Constraints {
	public class ClassConstraintOnMethod {
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
				Helper<DerivedFromConstraint> ();
			}

			[Kept]
			private static Container<T> Helper<T> () where T : class
			{
				return null;
			}

			[Kept]
			class Container<T> {
			}

			[Kept]
			class DerivedFromConstraint : Constraint {
			}
		}
	}
}