using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseProtectedStaticFieldFromSelf {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class Base {
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			protected static int Field;

			[Kept]
			public static void StaticMethod ()
			{
				Field = 1;
			}
		}
	}
}