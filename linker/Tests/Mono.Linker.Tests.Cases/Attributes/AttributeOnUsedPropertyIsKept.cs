using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnUsedPropertyIsKept
	{
		public static void Main()
		{
			var val = new A().Field;
		}

		class A
		{
			[Foo]
			public int Field { get; set; }
		}

		[Kept]
		class FooAttribute : Attribute
		{
		}
	}
}
