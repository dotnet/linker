﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{

#if !NETCOREAPP
	[IgnoreTestCase ("Can be enabled once MonoBuild produces a dll from which we can grab the types in the Mono.Linker namespace.")]
#else
	[SetupCompileBefore ("CustomStep.dll", new [] { "Dependencies/CustomStepDummy.cs" }, new [] { "illink.dll" })]
#endif
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepDummy,CustomStep.dll")]
	[SetupLinkerArgument ("--custom-step", "-CleanStep:CustomStep.CustomStepDummy,CustomStep.dll")]
	[SetupLinkerArgument ("--custom-step", "+CleanStep:CustomStep.CustomStepDummy,CustomStep.dll")]
	[LogContains ("Custom step added")]
	public class AddCustomStep
	{
		public static void Main ()
		{
		}
	}
}
