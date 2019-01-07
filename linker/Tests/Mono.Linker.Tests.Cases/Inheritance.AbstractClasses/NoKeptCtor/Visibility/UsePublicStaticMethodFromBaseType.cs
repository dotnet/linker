using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UsePublicStaticMethodFromBaseType {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			public static void Foo ()
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