using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class UseStaticFieldFromBaseType4 {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}
	}
	
	[Kept]
	abstract class UseStaticFieldFromBaseType4_Base {
		[Kept]
		protected static int Field;
	}

	[Kept]
	[KeptBaseType (typeof (UseStaticFieldFromBaseType4_Base))]
	class StaticMethodOnlyUsed : UseStaticFieldFromBaseType4_Base {
		[Kept]
		public static void StaticMethod ()
		{
			Field = 1;
		}
	}
}