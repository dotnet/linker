using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericConstraintOnMethod3 {
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
			
			[Kept]
			[KeptBaseType (typeof (NestedType))]
			protected class NestedType2 : NestedType {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType2))]
			protected class NestedType3 : NestedType2 {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType3))]
			protected class NestedType4 : NestedType3 {
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
			[KeptBaseType (typeof (NestedType2))]
			class DerivedFromNested2 : NestedType2 {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType3))]
			class DerivedFromNested3 : NestedType3 {
			}
			
			[Kept]
			[KeptBaseType (typeof (NestedType4))]
			class DerivedFromNested4 : NestedType4 {
			}
		}
	}
}