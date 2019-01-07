using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Explicit {
	public class SoftUpcast {
		public static void Main ()
		{
			var val = Helper () as CastType;
			Helper2 (val);
		}

		[Kept]
		static object Helper ()
		{
			return null;
		}
		
		[Kept]
		static void Helper2 (CastType arg)
		{
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