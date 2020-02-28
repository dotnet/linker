using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CommandLine
{
	[SetupCompileBefore ("CustomStep.dll", new [] { "Dependencies/CustomStepDummy.cs" })]
	[SetupLinkerArgument ("--custom-step", "CustomStep.CustomStepDummy", "CustomStep.dll")]
	[LogContains("Custom step added")]
	public class AddCustomStep
	{
		public static void Main ()
		{
		}
	}
}