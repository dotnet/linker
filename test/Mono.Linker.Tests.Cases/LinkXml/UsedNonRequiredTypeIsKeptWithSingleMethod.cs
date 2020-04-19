using System.Collections.Generic;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UsedNonRequiredTypeIsKeptWithSingleMethod {
		public static void Main ()
		{
			var t = typeof (Unused);
		}

		[Kept]
		class Unused
		{
			[Kept]
			private void PreservedMethod ()
			{
				new SecondLevel (2);
			}
		}

		[Kept]
		class SecondLevel
		{
			[Kept]
			public SecondLevel (int arg)
			{
			}
		}

		class ReallyUnused
		{
			private void PreservedMethod ()
			{
				new SecondLevelUnused (2);
			}
		}

		class SecondLevelUnused
		{
			public SecondLevelUnused (int arg)
			{
			}
		}
	}
}