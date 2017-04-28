using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo(Val = 1)]
	class AttributeOnPreservedTypeWithUsedSetter
	{
		public static void Main() { }

		class FooAttribute : Attribute
		{
			public int Val { [Removed] get; [Kept] set; }
		}
	}
}
