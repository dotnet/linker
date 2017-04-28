using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo(Val = typeof(A))]
	class AttributeOnPreservedTypeWithTypeUsedInFieldIsKept
	{
		public static void Main() { }

		class FooAttribute : Attribute
		{
			public Type Val;
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
