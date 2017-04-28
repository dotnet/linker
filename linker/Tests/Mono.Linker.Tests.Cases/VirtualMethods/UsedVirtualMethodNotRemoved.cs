using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.VirtualMethods
{
	public class UsedVirtualMethodNotRemoved
	{
		public static void Main()
		{
			new B(); new Base().Call();
		}

		class Base
		{
			[Kept]
			public virtual void Call() { }
		}

		class B : Base
		{
			[Kept]
			public override void Call() { }
		}
	}
}
