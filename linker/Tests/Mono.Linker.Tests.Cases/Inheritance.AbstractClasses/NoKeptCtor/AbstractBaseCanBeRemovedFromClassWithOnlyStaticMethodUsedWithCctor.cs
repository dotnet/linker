using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class AbstractBaseCanBeRemovedFromClassWithOnlyStaticMethodUsedWithCctor {
		public static void Main ()
		{
			UsedBase p = new UsedClass ();
			StaticMethodOnlyUsed.StaticMethod ();
			p.Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		abstract class UsedBase {
			[Kept]
			public abstract void Foo ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UsedBase))]
		class UsedClass : UsedBase {
			[Kept]
			public override void Foo ()
			{
			}
		}

		[Kept]
		class StaticMethodOnlyUsed : UsedBase {
			[Kept]
			static StaticMethodOnlyUsed()
			{
				UsedByStaticCctor();
			}

			[Kept]
			static void UsedByStaticCctor()
			{
			}
			
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