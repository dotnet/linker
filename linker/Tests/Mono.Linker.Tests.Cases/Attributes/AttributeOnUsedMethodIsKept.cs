using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Attributes
{
	class AttributeOnUsedMethodIsKept
	{
		public static void Main()
		{
			new A().Method();
		}

		class A
		{
			[Foo]
			public void Method()
			{
			}
		}

		[Kept]
		class FooAttribute : Attribute
		{
		}
	}
}
