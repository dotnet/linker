using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericOnMethodWithConstraint {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept] // Could be removed with improved base sweeping
		abstract class Base {
			[Kept] // Could be removed with improved base sweeping
			protected class NestedType {
				public void Foo ()
				{
				}
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))] // Could be removed with improved base sweeping
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper<DerivedFromNested> ();
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
			[KeptBaseType (typeof (NestedType))] // Could be removed with improved base sweeping 
			class DerivedFromNested : NestedType {
			}
		}
	}
}