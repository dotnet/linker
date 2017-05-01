using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.References {
	[IgnoreTestCase ("TODO by Mike : This test needs to be implemented still")]
	// Need a way to define additional references
	// Need a way to assert a reference is removed
	class ReferencesAreRemovedWhenAllUsagesAreRemoved {
		public static void Main ()
		{
		}

		class UnusedClassThatUsesTypeFromAnotherAssembly {
			void SomeMethod ()
			{
				// Use something in a different assembly
			}
		}
	}
}