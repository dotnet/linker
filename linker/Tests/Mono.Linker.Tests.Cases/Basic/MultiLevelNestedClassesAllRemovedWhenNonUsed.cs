using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class MultiLevelNestedClassesAllRemovedWhenNonUsed
	{
		public static void Main()
		{
		}

		[Removed]
		public class A
		{
			[Removed]
			public class AB
			{
				[Removed]
				public class ABC
				{
				}

				[Removed]
				public class ABD
				{
				}
			}
		}
	}
}
