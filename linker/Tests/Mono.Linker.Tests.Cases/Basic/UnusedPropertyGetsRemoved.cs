using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedPropertyGetsRemoved
	{
		public static void Main()
		{
			new UnusedPropertyGetsRemoved.B().Method();
		}

		class B
		{
			[Removed]
			public int Unused { get; set; }

			public void Method() { }
		}
	}
}
