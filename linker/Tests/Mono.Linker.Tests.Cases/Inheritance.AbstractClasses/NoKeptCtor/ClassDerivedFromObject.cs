using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor {
	public class ClassDerivedFromObject {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}
		
		[Kept]
		class StaticMethodOnlyUsed {
			[Kept]
			public static void StaticMethod ()
			{
			}
		}
	}
}