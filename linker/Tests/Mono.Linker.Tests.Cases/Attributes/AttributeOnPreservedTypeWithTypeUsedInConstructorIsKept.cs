using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo(typeof(A))]
	class AttributeOnPreservedTypeWithTypeUsedInConstructorIsKept
	{
		public static void Main() { }

		class FooAttribute : Attribute
		{
			public FooAttribute(Type val)
			{
			}
		}

		[Kept]
		class A
		{
			[Removed]
			public A()
			{
			}
		}
	}
}
