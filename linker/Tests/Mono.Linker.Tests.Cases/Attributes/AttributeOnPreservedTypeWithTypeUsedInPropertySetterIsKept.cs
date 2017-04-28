using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo(Val = typeof(A))]
	class AttributeOnPreservedTypeWithTypeUsedInPropertySetterIsKept
	{
		public static void Main() { }

		class FooAttribute : Attribute
		{
			public Type Val { get; set; }
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
