using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedMethodGetsRemoved
	{
		public static void Main()
		{
			new UnusedMethodGetsRemoved.B().Method();
		}

		class B
		{
			[Removed]
			public void Unused() { }

			public void Method() { }
		}
	}
}
