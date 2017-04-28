using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	class NestedDelegateInvokeMethodsPreserved
	{
		static B.Delegate @delegate;

		static void Main() { System.GC.KeepAlive(@delegate); }

		[Kept]
		public class B
		{
			[Kept]
			[KeptMember("Invoke()")]
			public delegate void Delegate();
		}
	}
}
