using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.References
{
	[IgnoreTestCase("TODO by Mike : Needs to be implemented still")]
	// Need a way to define additional references
	// Need a way to assert a reference is removed
	class ReferencesAreRemovedWhenAllUsagesAreRemoved
	{
		public static void Main()
		{
		}

		class UnusedClassThatUsesTypeFromAnotherAssembly
		{
			void SomeMethod()
			{
				// Use something in a different assembly
			}
		}
	}
}
