using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	class AttributeOnUsedFieldIsKept {
		public static void Main ()
		{
			var val = new A ().field;
		}

		[KeptMember (".ctor()")]
		class A {
			[Kept]
			[Foo] public int field;
		}

		[Kept]
		[KeptMember(".ctor()")]
		[KeptBaseType ("System.Attribute")]
		class FooAttribute : Attribute {
		}
	}
}