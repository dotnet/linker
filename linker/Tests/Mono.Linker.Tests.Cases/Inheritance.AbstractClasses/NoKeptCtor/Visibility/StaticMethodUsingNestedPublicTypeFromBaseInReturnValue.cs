using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticMethodUsingNestedPublicTypeFromBaseInReturnValue {
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
		[KeptBaseType (typeof (Base))] // Could be removed with improved base sweeping since `NestedType` is public
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Helper();
			}

			[Kept]
			private static NestedType Helper ()
			{
				return null;
			}
		}
	}
}