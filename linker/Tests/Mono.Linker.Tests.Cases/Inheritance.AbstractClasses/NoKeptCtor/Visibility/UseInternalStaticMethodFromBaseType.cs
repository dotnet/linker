using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseInternalStaticMethodFromBaseType {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			internal static void Foo ()
			{
			}
		}

		[Kept]
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				Foo ();
			}
		}
	}
}