using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseStaticMethodFromBaseType2 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class One {
			[Kept]
			protected static void Foo ()
			{
			}
		}
		
		[Kept]
		[KeptBaseType (typeof (One))]
		abstract class Two : One {
			[Kept]
			protected static void Bar ()
			{
			}
		}
		
		[Kept]
		[KeptBaseType (typeof (Two))]
		abstract class Three : Two {
			[Kept]
			protected static void Jar ()
			{
			}
		}
		
		[Kept]
		[KeptBaseType (typeof (Three))]
		abstract class Four : Three {
			[Kept]
			protected static void Car ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Four))]
		class StaticMethodOnlyUsed : Four {
			[Kept]
			public static void StaticMethod ()
			{
				Foo ();
				Bar ();
				Jar ();
				Car ();
			}
		}
	}
}