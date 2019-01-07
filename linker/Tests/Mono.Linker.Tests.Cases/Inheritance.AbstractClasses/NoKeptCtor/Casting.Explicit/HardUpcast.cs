using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Explicit {
	public class HardUpcast {
		public static void Main ()
		{
			var val = (CastType) Helper ();
		}

		[Kept]
		static object Helper()
		{
			return null;
		}

		[Kept]
		class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class CastType : Base {
		}
	}
}