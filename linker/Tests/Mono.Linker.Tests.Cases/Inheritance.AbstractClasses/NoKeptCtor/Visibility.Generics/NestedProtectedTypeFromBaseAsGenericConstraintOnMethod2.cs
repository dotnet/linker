using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericConstraintOnMethod2 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			protected class NestedType {
				public void Foo ()
				{
				}
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper<DerivedFromNested> ();
				Helper<DerivedFromNested2> ();
				Helper<DerivedFromNested3> ();
				Helper<DerivedFromNested4> ();
			}

			[Kept]
			private static Container<T> Helper<T> () where T : NestedType
			{
				return null;
			}
			
			[Kept]
			class Container<T> {
			}

			[Kept]
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested : NestedType {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested2 : NestedType {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested3 : NestedType {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			class DerivedFromNested4 : NestedType {
			}
		}
	}
}