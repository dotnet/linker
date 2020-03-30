using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerArgument ("--set-property", "Mono.Linker.Tests.Cases.Substitutions.ConstantProperty.BoolProperty", "true")]
	public class ConstantProperty
	{
		[Kept]
		static bool BoolProperty {
			[Kept]
			[ExpectedInstructionSequence (new [] {
				"ldc.i4.1",
				"ret"
			})]
			get;
		}

		public static void Main()
		{
			TestProperty_1 ();
		}

		[Kept]
		static bool TestProperty_1 ()
		{
			return BoolProperty;
		}
	}
}
