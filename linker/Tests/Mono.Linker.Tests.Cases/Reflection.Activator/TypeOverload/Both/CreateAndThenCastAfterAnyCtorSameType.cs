using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Both {
	public class CreateAndThenCastAfterAnyCtorSameType {
		public static void Main ()
		{
			// This will cause ctor() to be marked
			System.Activator.CreateInstance (typeof (Foo));
			
			// This second usage will now cause all ctors to be marked
			var tmp = System.Activator.CreateInstance (UndetectableWayOfGettingType (), new object [0]) as Foo;
			HereToUseCreatedInstance (tmp);
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}
		
		[Kept]
		static Type UndetectableWayOfGettingType ()
		{
			return null;
		}

		[Kept]
		class Foo {
			[Kept]
			public Foo ()
			{
			}

			[Kept]
			public Foo (int arg)
			{
			}
		}
	}
}