using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupCompileResource ("Dependencies/EmbeddedSubstitutions.xml", "ILLink.Substitutions.xml")]
	[IgnoreSubstitutions (false)]
	[RemovedResourceInAssembly ("test.exe", "ILLink.Substitutions.xml")]
	public class EmbeddedSubstitutions
	{
		public static void Main ()
		{
			ConvertToThrowMethod ();
		}

		[Kept]
		[ExpectedInstructionSequence (new[] {
			"ldstr",
			"newobj",
			"throw"
		})]
		public static void ConvertToThrowMethod ()
		{
		}
	}
}
