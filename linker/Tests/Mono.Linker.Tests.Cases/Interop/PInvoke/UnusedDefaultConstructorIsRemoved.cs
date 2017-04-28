using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke
{
	class UnusedDefaultConstructorIsRemoved
	{
		public static void Main()
		{
			var a = new A(1);
			SomeMethod(a);
		}

		class A
		{
			[Removed]
			public A() { }

			public A(int other)
			{
			}
		}

		[DllImport("Unused")]
		private static extern void SomeMethod(A a);
	}
}
