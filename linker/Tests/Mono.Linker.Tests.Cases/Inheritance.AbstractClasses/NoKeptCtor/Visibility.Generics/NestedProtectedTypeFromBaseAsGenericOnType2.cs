using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericOnType2 {
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
				new NestedInStatic<NestedType> ().Method ();
			}

			// Needs a base type to trigger a more complex scenario
			[Kept]
			[KeptMember (".ctor()")]
			[KeptBaseType (typeof (BaseInStatic))]
			class NestedInStatic<NestedType> : BaseInStatic {
				[Kept]
				public void Method ()
				{
					var tmp = typeof (NestedType).ToString ();
				}
			}

			[Kept]
			[KeptMember (".ctor()")]
			class BaseInStatic {
			}
		}
	}
}