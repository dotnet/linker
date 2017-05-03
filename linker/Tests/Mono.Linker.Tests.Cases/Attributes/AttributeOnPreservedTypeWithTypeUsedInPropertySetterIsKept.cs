using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes {
	[Foo (Val = typeof (A))]
	class AttributeOnPreservedTypeWithTypeUsedInPropertySetterIsKept {
		public static void Main ()
		{
		}

		[KeptMember ("<Val>k__BackingField")]
		[KeptMember (".ctor()")]
		[KeptBaseType ("System.Attribute")]
		class FooAttribute : Attribute {
			[Kept]
			public Type Val { get; [Kept] set; }
		}

		[Kept]
		class A {
			public A ()
			{
			}
		}
	}
}