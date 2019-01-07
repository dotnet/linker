using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Explicit {
	public class SoftUpcast2 {
		public static void Main ()
		{
			var val = Helper () as CastType;
			Helper2 (val);
		}

		[Kept]
		static void Helper2 (CastType arg)
		{
		}

		[Kept]
		static Base Helper()
		{
			return null;
		}

		[Kept]
		class Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class Base2 : Base {
		}

		[Kept]
		[KeptBaseType (typeof (Base2))]
		class Base3 : Base2 {
		}

		[Kept]
		[KeptBaseType (typeof (Base3))]
		class CastType : Base3 {
		}
	}
}