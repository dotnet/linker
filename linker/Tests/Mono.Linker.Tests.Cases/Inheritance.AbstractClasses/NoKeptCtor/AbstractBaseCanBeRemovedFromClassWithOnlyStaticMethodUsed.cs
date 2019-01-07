using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class AbstractBaseCanBeRemovedFromClassWithOnlyStaticMethodUsed {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		abstract class UnusedBase {
			public abstract void Foo ();
		}

		[Kept]
		class StaticMethodOnlyUsed : UnusedBase {
			public override void Foo ()
			{
			}

			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}