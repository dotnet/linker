using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnUsedFieldIsKept
	{
		public static void Main()
		{
			var val = new A().field;
		}

		class A
		{
			[Foo]
			public int field;
		}

		[Kept]
		class FooAttribute : Attribute
		{
		}
	}
}
