using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	class UnusedPropertyPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember ("<PreservedProperty1>k__BackingField")]
		[KeptMember ("<PreservedProperty2>k__BackingField")]
		[KeptMember ("<PreservedProperty3>k__BackingField")]
		class Unused {
			[Kept]
			public int PreservedProperty1 { [Kept] get; [Kept] set; }

			[Kept]
			public int PreservedProperty2 { [Kept] get; set; }

			[Kept]
			public int PreservedProperty3 { get; [Kept] set; }

			public int NotPreservedProperty { get; set; }
		}
	}
}