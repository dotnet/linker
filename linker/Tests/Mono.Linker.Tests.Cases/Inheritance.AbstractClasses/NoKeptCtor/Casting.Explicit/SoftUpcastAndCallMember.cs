using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses.NoKeptCtor.Casting.Explicit {
	public class SoftUpcastAndCallMember {
		public static void Main ()
		{
			CastType val = Helper () as CastType;
			val.MethodFromBase ();
		}

		[Kept]
		static Base Helper ()
		{
			return null;
		}

		[Kept]
		class Base {
			[Kept]
			public void MethodFromBase ()
			{
			}
		}

		[Kept]
		[KeptBaseType (typeof (Base))]
		class CastType : Base {
		}
	}
}