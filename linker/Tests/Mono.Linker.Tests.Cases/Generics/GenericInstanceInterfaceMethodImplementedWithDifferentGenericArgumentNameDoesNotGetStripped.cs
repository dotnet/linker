using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics
{
	class GenericInstanceInterfaceMethodImplementedWithDifferentGenericArgumentNameDoesNotGetStripped
	{
		public static void Main()
		{
			ISomething it = new Concrete();
			it.ShouldNotGetStripped<int>();
		}

		public class GenericType<T>
		{
		}

		public interface ISomething
		{
			GenericType<TInInterface> ShouldNotGetStripped<TInInterface>();
		}

		public class Concrete : ISomething
		{
			[Kept]
			public GenericType<TInConcrete> ShouldNotGetStripped<TInConcrete>()
			{
				throw new System.Exception();
			}
		}
	}
}
