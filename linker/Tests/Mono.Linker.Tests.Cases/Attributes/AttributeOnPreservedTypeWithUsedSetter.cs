using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	[Foo (Val = 1)]
	class AttributeOnPreservedTypeWithUsedSetter {
		public static void Main ()
		{
		}

		[KeptMember ("<Val>k__BackingField")]
		[KeptMember (".ctor()")]
		[KeptBaseType ("System.Attribute")]
		class FooAttribute : Attribute {
			[Kept]
			public int Val { get; [Kept] set; }
		}
	}
}