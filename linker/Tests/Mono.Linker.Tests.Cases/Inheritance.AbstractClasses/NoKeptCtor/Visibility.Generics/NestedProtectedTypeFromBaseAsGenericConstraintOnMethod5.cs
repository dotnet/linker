using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericConstraintOnMethod5 {
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
		abstract class Base2 : Base {
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			protected class NestedType2 : NestedType {
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		abstract class Base3 : Base2 {
			[Kept]
			[KeptBaseType (typeof (NestedType2))]
			protected class NestedType3 : NestedType2 {
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		class StaticMethodOnlyUsed : Base3 {
			[Kept]
			public static void StaticMethod ()
			{
				Helper<DerivedFromNested> ();
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
			[KeptBaseType (typeof (NestedType3))]
			class DerivedFromNested : NestedType3 {
			}
		}
	}
}