using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;


namespace Mono.Linker.Tests.Cases.Logging {

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("LogStep.dll", new [] { "Dependencies/LogStep.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "Log.LogWarningStep,LogStep.dll")]
	[LogContains ("illinker: error L100: error")]
	[LogContains ("illinker: warning L400: warning")]
	[LogContains ("illinker: info L900: info")]
	public class CommonLogs
	{
		public static void Main ()
		{
		}
	}
}
