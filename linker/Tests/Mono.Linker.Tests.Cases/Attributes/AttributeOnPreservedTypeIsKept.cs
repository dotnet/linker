using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo]
	class AttributeOnPreservedTypeIsKept
	{
		public static void Main() { }

		[Kept]
		class FooAttribute : Attribute
		{
			[Kept]
			public FooAttribute() { }
		}
	}
}
