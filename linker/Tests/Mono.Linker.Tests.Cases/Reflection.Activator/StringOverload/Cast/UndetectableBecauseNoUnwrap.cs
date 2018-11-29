using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Cast {
	public class UndetectableBecauseNoUnwrap {
		public static void Main ()
		{
			HereToMarkFoo (null);
			System.Activator.CreateInstance (UndetectableAssemblyName (), UndetectableTypeName ());
		}

		[Kept]
		static void HereToMarkFoo (Foo arg)
		{
		}

		[Kept]
		static string UndetectableAssemblyName ()
		{
			return null;
		}

		[Kept]
		static string UndetectableTypeName ()
		{
			return null;
		}

		[Kept]
		class Foo {
		}
	}
}