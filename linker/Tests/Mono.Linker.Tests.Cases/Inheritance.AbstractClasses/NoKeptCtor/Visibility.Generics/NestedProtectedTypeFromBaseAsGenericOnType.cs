using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility.Generics {
	public class NestedProtectedTypeFromBaseAsGenericOnType {
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

			[Kept]
			[KeptMember (".ctor()")]
			class NestedInStatic<NestedType> {
				[Kept]
				public void Method ()
				{
					var tmp = typeof (NestedType).ToString ();
				}
			}
		}
	}
}