using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseStaticMethodFromBaseType3 {
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
		}
		
		[Kept]
		[KeptBaseType (typeof (Two))]
		abstract class Three : Two {
		}
		
		[Kept]
		[KeptBaseType (typeof (Three))]
		abstract class Four : Three {
		}

		// Technically the base type could be changed to One and all types in between can be removed
		[Kept]
		[KeptBaseType (typeof (Four))]
		class StaticMethodOnlyUsed : Four {
			[Kept]
			public static void StaticMethod ()
			{
				Foo ();
			}
		}
	}
}