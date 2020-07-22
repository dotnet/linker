using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerSubstitutionFile ("CanWarnAsErrorSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror-")]
	[SetupLinkerArgument ("--warnaserror+", "IL2011,IgnoreThis")]
	[SetupLinkerArgument ("--warnaserror", "IL2010,CS4321,IgnoreThisToo")]
	[LogContains ("warning IL20(07|08|09|10)", true)]
	[LogContains ("error IL20(11|12)", true)]
	public class CanWarnAsError
	{
		public static void Main ()
		{
		}

		class HelperClass
		{
			private int helperField = 0;
			int HelperMethod ()
			{
				return 0;
			}
		}
	}
}
