using System.Reflection;
using System.Timers;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Attributes {
	[Reference ("System.dll")]
	[SetupLinkerCoreAction ("link")]
#if NETCOREAPP
	[KeptAttributeInAssembly ("System.Private.CoreLib.dll", typeof (AssemblyDescriptionAttribute))]
	[KeptAttributeInAssembly ("System.Private.CoreLib.dll", typeof (AssemblyCompanyAttribute))]
	// System.dll isn't kept
#else
	[KeptAttributeInAssembly ("mscorlib.dll", typeof (AssemblyDescriptionAttribute))]
	[KeptAttributeInAssembly ("mscorlib.dll", typeof (AssemblyCompanyAttribute))]
	[KeptAttributeInAssembly ("System.dll", typeof (AssemblyDescriptionAttribute))]
	[KeptAttributeInAssembly ("System.dll", typeof (AssemblyCompanyAttribute))]
#endif
	[SkipPeVerify]
	public class CoreLibraryAssemblyAttributesAreKept {
		public static void Main ()
		{
			// Use something from System so that the entire reference isn't linked away
			var system = new Timer ();
		}
	}
}