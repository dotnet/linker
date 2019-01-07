using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticFieldWithNestedPublicGenericFromBase {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			public class NestedType {
				public void Foo ()
				{
				}
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))] // Could be removed with improved base sweeping
		class StaticMethodOnlyUsed : Base {
			[Kept]
			private static Container<NestedType> field;
			
			[Kept]
			public static void StaticMethod ()
			{
				field = null;
			}

			[Kept]
			class Container<T> {
			}
		}
	}
}