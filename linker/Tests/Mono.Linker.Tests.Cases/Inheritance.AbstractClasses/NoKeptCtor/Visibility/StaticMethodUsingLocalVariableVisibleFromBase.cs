using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Visibility {
	public class StaticMethodUsingLocalVariableVisibleFromBase {
		public static void Main ()
		{
			StaticMethodOnlyUsed.StaticMethod ();
		}

		[Kept]
		abstract class Base {
			[Kept]
			protected class NestedType {
			}
		}

		// Apparently it is valid IL to have a local of a type that shouldn't be visible to your type
		[Kept]
		[KeptBaseType (typeof (Base))] // Could be removed with improved base sweeping
		class StaticMethodOnlyUsed : Base {
			[Kept]
			public static void StaticMethod ()
			{
				NestedType tmp = null;
				// Do enough to prevent the compiler from optimizing away the local
				if (tmp == null)
					Helper ();
			}

			[Kept]
			static void Helper ()
			{
			}
		}
	}
}