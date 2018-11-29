using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Cast {
	public class DetectedByCast {
		public static void Main ()
		{
			var tmp = System.Activator.CreateInstance (UndetectableAssemblyName (), UndetectableTypeName ()).Unwrap () as Foo;
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
		[KeptMember (".ctor()")]
		class Foo {
		}
	}
}