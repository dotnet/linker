using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UnusedPropertySetterRemoved
	{
		public static void Main()
		{
			var val = new UnusedPropertySetterRemoved.B().PartiallyUsed;
		}

		class B
		{
			public int PartiallyUsed { [Kept] get; [Removed] set; }
		}
	}
}
