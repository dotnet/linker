using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.AbstractClasses {
	public class AllBasesKeptWhenExternInstanceCtorUsed {
		public static void Main ()
		{
			var f = new Foo ();
		}
		
		[Kept]
		abstract class One {
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

		[Kept]
		[KeptBaseType (typeof (Four))]
		abstract class Five : Four {
		}

		[Kept]
		[KeptBaseType (typeof (Five))]
		class Foo : Five {
			[Kept]
			[MethodImpl (MethodImplOptions.InternalCall)]
			public extern Foo ();
		}
	}
}