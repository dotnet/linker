using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class UsedPropertyIsKept
	{
		public static void Main()
		{
			var obj = new B();
			obj.Prop = 1;
			var val = obj.Prop;
		}

		class B
		{
			[Kept]
			public int Prop { get; set; }
		}
	}
}
