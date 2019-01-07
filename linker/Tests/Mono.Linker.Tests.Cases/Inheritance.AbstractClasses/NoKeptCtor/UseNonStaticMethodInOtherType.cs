using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class UseNonStaticMethodInOtherType {
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
				new Other ().Foo ();
			}
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Other {
			[Kept]
			public void Foo()
			{
			}
		}
	}
}