using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace NamespaceForAttributeOnPreservedTypeWithTypeUsedInDifferentNamespaceIsKept
{
	[Kept]
	class A
	{
		[Removed]
		public A()
		{
		}
	}
}

namespace Mono.Linker.Tests.Cases.Attributes
{
	[Foo(typeof(NamespaceForAttributeOnPreservedTypeWithTypeUsedInDifferentNamespaceIsKept.A))]
	class AttributeOnPreservedTypeWithTypeUsedInDifferentNamespaceIsKept
	{
		public static void Main() { }

		class FooAttribute : Attribute
		{
			public FooAttribute(Type val)
			{
			}
		}

		// This A is not used and should be removed
		[Removed]
		class A
		{
		}
	}
}
