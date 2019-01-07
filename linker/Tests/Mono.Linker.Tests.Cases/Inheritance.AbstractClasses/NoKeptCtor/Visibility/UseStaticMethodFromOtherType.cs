using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseStaticMethodFromOtherType {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class Base {
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Other.Foo ();
			}
		}

		[Kept]
		class Other {
			[Kept]
			public static void Foo ()
			{
			}
		}
	}
}