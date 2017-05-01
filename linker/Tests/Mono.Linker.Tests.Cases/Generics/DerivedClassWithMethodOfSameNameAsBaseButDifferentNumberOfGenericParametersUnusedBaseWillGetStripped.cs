using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Generics {
	class DerivedClassWithMethodOfSameNameAsBaseButDifferentNumberOfGenericParametersUnusedBaseWillGetStripped {
		public static void Main (string[] args)
		{
			MyDerived obj = new MyDerived ();
			obj.Method<int, int> (1);
		}

		class MyBase {
			[Removed]
			public virtual T Method<T> (T arg1)
			{
				return arg1;
			}
		}

		class MyDerived : MyBase {
			[Kept]
			public virtual T Method<T, K> (T arg1)
			{
				return arg1;
			}
		}
	}
}