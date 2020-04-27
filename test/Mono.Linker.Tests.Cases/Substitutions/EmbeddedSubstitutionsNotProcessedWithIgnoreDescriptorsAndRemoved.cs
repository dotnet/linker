using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileResource ("Dependencies/EmbeddedSubstitutionsNotProcessedWithIgnoreDescriptorsAndRemoved.xml", "ILLink.Substitutions.xml")]
	[IgnoreDescriptors (true)]
	[StripResources (true)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.Substitutions.xml")]
	public class EmbeddedSubstitutionsNotProcessedWithIgnoreDescriptorsAndRemoved
	{
		public static void Main ()
		{
			ConvertToThrowMethod ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"nop",
			"ret"
		})]
		public static void ConvertToThrowMethod ()
		{
		}
	}
}
