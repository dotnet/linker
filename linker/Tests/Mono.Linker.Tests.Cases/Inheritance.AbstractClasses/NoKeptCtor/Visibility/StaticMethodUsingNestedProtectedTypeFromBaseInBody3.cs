using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticMethodUsingNestedProtectedTypeFromBaseInBody3 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			protected class NestedType {
				[Kept]
				public class NestedType2 {
				}
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				if (GetAValue () is NestedType.NestedType2)
				{
					Helper ();
				}
			}

			[Kept]
			static void Helper ()
			{
			}

			[Kept]
			static object GetAValue ()
			{
				return null;
			}
		}
	}
}