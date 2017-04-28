using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Interop.InternalCalls
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

		[MethodImpl(MethodImplOptions.InternalCall)]
		static extern void SomeMethod(A a);
	}
}
