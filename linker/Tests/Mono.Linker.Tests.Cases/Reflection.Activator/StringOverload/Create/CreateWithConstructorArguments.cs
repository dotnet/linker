
namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Create {
	/// <summary>
	/// Without full stack evaluation it's not possible to find the ldstr's
	/// </summary>
	public class CreateWithConstructorArguments {
		public static void Main ()
		{
			System.Activator.CreateInstance ("test", "Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Create.CreateWithConstructorArguments+Foo", new object [0]);
		}

		class Foo {
			//[Kept] Requires better stack evaluation
			public Foo ()
			{
			}

			//[Kept] Requires better stack evaluation
			public Foo (int arg)
			{
			}
		}
	}
}