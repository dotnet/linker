using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType.NoKeptCtorButInterfaceNeeded {
	public class ParameterRefAndLocal {
		public static void Main ()
		{
			Foo f = null;
			Helper (ref f);
		}

		[Kept]
		static void Helper (ref Foo f)
		{
			IFoo i = f;
			i.Method ();
		}

		[Kept]
		interface IFoo {
			[Kept]
			void Method ();
		}

		[Kept]
		[KeptInterface (typeof (IFoo))]
		class Foo : IFoo {
			[Kept]
			public void Method ()
			{
			}
		}
	}
}