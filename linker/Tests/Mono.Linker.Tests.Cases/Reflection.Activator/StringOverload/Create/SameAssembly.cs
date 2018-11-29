using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Create {
	public class SameAssembly {
		public static void Main ()
		{
			System.Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Create.SameAssembly+Foo");
		}

		[Kept]
		class Foo {
			[Kept]
			public Foo ()
			{
			}

			public Foo (int arg)
			{
			}
		}
	}
}