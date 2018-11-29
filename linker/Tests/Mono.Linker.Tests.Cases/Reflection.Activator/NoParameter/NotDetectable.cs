using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.NoParameter {
	public class NotDetectable {
		public static void Main ()
		{
			var tmp = Create<Foo> ();
			HereToUseCreatedInstance (tmp);
		}

		[Kept]
		static T Create<T>()
		{
			return System.Activator.CreateInstance<T> ();
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}

		[Kept]
		class Foo {
		}
	}
}