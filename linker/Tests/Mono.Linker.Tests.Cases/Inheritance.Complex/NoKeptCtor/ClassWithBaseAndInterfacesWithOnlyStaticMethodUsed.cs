using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Complex.NoKeptCtor {
	public class ClassWithBaseAndInterfacesWithOnlyStaticMethodUsed {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class UnusedBase {
			public abstract void Foo ();
		}

		interface IFoo {
			void Method1 ();
		}

		interface IBar {
			void Method2 ();
		}

		[Kept]
		class StaticMethodOnlyUsed : UnusedBase, IBar, IFoo {
			public override void Foo ()
			{
			}

			[Kept]
			public static void StaticMethod ()
			{
			}

			public void Method2 ()
			{
			}

			public void Method1 ()
			{
			}
		}
	}
}