using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging {

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("LogStep.dll", new [] { "Dependencies/LogStep.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "Log.LogStep,LogStep.dll")]
	[LogContains ("logtest: error L100: Error")]
	[LogContains ("logtest: warning L400: Warning")]
	[LogContains ("illinker: info L900: Info")]
	public class CommonLogs
	{
		public static void Main ()
		{
		}
	}
}
