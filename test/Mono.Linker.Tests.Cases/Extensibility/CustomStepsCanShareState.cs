using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
	[SetupCompileBefore ("customstep.dll", new[] {
		"Dependencies/CustomStepsWithSharedState.cs",
		"Dependencies/PreserveMembersSubStep.cs"
		}, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "SharedStateHandler2,customstep.dll")]
	[SetupLinkerArgument ("--custom-step", "SharedStateHandler1,customstep.dll")]
	public class CustomStepsCanShareState
	{
		public static void Main ()
		{
		}

		[Kept]
		public static void MarkedMethod ()
		{
		}

		public static void UnmarkedMethod ()
		{
		}
	}
}