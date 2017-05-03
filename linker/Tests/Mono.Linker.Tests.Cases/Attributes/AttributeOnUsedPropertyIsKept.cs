using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	class AttributeOnUsedPropertyIsKept {
		public static void Main ()
		{
			var val = new A ().Field;
		}

		[KeptMember (".ctor()")]
		[KeptMember ("<Field>k__BackingField")]
		[KeptMember ("get_Field()")]
		class A {
			[Kept]
			[Foo]
			public int Field { get; set; }
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType ("System.Attribute")]
		class FooAttribute : Attribute {
		}
	}
}