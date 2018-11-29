using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.TypeOverload.Both {
	public class NoDefaultCtorAndCastToSameType {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (typeof (Foo)) as Foo;
			HereToUseCreatedInstance (tmp);
		}
		
		[Kept]
		static void HereToUseCreatedInstance (object arg)
		{
		}

		[Kept]
		class Foo {
			// Should not be marked because we detect the create type and this CreateInstance usage is for the default ctor only
			public Foo (int arg)
			{
			}
		}
	}
}