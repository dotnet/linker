using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class AbstractBaseCanBeRemovedFromClassWithOnlyStaticMethodUsedMultiBase {
		public static void Main ()
		{
			UsedBase1 p = new UsedClass ();
			StaticMethodOnlyUsed.StaticMethod ();
			p.Foo ();
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		abstract class UsedBase1 {
			[Kept]
			public abstract void Foo ();
		}
		
		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UsedBase1))]
		abstract class UsedBase2 : UsedBase1 {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UsedBase2))]
		abstract class UsedBase3 : UsedBase2 {
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (UsedBase3))]
		class UsedClass : UsedBase3 {
			[Kept]
			public override void Foo ()
			{
			}
		}

		[Kept]
		class StaticMethodOnlyUsed : UsedBase3 {
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