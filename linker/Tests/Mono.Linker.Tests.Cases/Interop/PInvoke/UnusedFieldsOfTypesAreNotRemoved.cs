using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke
{
	class UnusedFieldsOfTypesAreNotRemoved
	{
		public static void Main()
		{
			var a = new A();
			SomeMethod(a);
		}

		class A
		{
			[Kept]
			private int field1;

			[Kept]
			private int field2;
		}

		[DllImport("Unused")]
		private static extern void SomeMethod(A a);
	}
}
