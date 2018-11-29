using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Cast {
	public class UndetectableDueToUnwrap {
		public static void Main ()
		{
			var handle = System.Activator.CreateInstance (UndetectableAssemblyName (), UndetectableTypeName ());
			HereToUseCreatedInstance (handle);
			var tmp = handle.Unwrap () as Foo;
			HereToUseCreatedInstance (tmp);
		}

		[Kept]
		static void HereToUseCreatedInstance (object arg)
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