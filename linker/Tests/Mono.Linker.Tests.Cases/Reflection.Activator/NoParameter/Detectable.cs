using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.NoParameter {
	public class Detectable {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance<Foo> ();
			HereToUseCreatedInstance (tmp);
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		class Foo {
		}
	}
}