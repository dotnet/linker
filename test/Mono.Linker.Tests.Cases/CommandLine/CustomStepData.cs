using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("CustomStep.dll", new [] { "Dependencies/CustomStepUser.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepUser,CustomStep.dll")]
	[SetupLinkerArgument ("--custom-data", "NewKey=UserValue")]

	[LogContains ("Custom step added with custom data of UserValue")]
	public class CustomStepData
	{
		public static void Main ()
		{
		}
	}
}
