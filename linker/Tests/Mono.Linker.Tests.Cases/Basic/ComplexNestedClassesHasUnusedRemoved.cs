using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class ComplexNestedClassesHasUnusedRemoved
	{
		public static void Main()
		{
			new A.AB.ABD();
		}

		public class A
		{
			public class AB
			{
				[Removed]
				public class ABC
				{
				}

				[Kept]
				public class ABD
				{
					[Removed]
					public class ABDE
					{
					}
				}
			}
		}
	}
}
