using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericOnMethodInOtherType {
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
				OtherType.Method<NestedType> ();
			}
		}

		[Kept]
		class OtherType {
			[Kept]
			public static void Method<T> ()
			{
			}
		}
	}
}