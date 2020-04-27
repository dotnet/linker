using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileResource ("Dependencies/EmbeddedSubstitutionsNotProcessedWithIgnoreDescriptors.xml", "ILLink.Substitutions.xml")]
	[IgnoreDescriptors (true)]
	[StripResources (false)]
	[KeptResource ("ILLink.Substitutions.xml")]
	public class EmbeddedSubstitutionsNotProcessedWithIgnoreDescriptors
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
