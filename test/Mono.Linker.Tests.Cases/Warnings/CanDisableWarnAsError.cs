using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings
{
	[SkipKeptItemsValidation]
	[SetupLinkerSubstitutionFile ("CanDisableWarnAsErrorSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--warnaserror")]
	[SetupLinkerArgument ("--warnaserror-", "IL2011,IL2012,IgnoreThis")]
	[LogContains ("error IL20(07|08|09|10)", true)]
	[LogContains ("warning IL20(11|12)", true)]
	public class CanDisableWarnAsError
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
