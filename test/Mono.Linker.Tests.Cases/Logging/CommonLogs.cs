using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Logging
{

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("LogStep.dll", new [] { "Dependencies/LogStep.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "Log.LogStep,LogStep.dll")]

	[LogContainsExact ("illinker: error IL1000: Error")]
	[LogContainsExact ("illinker: warning IL4000: Warning")]
	[LogContainsExact ("logtest(1,1): info IL9000")]
	public class CommonLogs
	{
		public static void Main ()
		{
		}
	}
}
