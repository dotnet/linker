using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic {
	class UnusedPropertySetterRemoved {
		public static void Main ()
		{
			var val = new UnusedPropertySetterRemoved.B ().PartiallyUsed;
		}

		[KeptMember (".ctor()")]
		[KeptMember ("<PartiallyUsed>k__BackingField")]
		class B {
			[Kept] // FIXME: Should be removed
			public int PartiallyUsed { [Kept] get; set; }
		}
	}
}