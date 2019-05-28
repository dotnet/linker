using System.Reflection;
using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed {
	[Reference ("System.dll")]
	[SetupLinkerCoreAction ("link")]
	[SetupLinkerArgument ("--used-attrs-only", "true")]
#if NETCOREAPP
	[IgnoreTestCase ("AssemblyDescriptionAttribute was removed from CoreLib.")]
#endif
	[KeptAttributeInAssembly (PlatformAssemblies.CoreLib, typeof (AssemblyDescriptionAttribute))]
#if !NETCOREAPP
	[KeptAttributeInAssembly ("System.dll", typeof (AssemblyDescriptionAttribute))]
#endif
	[SkipPeVerify]
	public class CoreLibraryUsedAssemblyAttributesAreKept {
		public static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();

			// use one of the attribute types so that it is marked
			var tmp = typeof (AssemblyDescriptionAttribute).ToString ();
		}
	}
}