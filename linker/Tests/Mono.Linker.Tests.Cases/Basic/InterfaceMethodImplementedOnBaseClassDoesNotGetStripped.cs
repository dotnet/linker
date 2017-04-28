using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class InterfaceMethodImplementedOnBaseClassDoesNotGetStripped
	{
		public static void Main()
		{
			I1 i1 = new Derived();
			i1.Used();
		}

		public interface I1
		{
			void Unused();
			void Used();
		}

		public class Base
		{
			[Removed]
			public void Unused() { }

			[Kept]
			public void Used() { }
		}

		public class Derived : Base, I1
		{
		}
	}
}
