using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	public class UsedOverloadedGenericMethodInGenericClassIsNotStripped
	{
		public static void Main()
		{
			B<int>.Method(true);
		}

		class B<TBase>
		{
			[Kept]
			public static void Method<T>(T value) { }

			[Removed]
			public static void Method(TBase value) { }
		}
	}
}
