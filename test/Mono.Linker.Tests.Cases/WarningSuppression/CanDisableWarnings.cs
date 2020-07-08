using System;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[SkipKeptItemsValidation]
	[SetupLinkerSubstitutionFile ("NoWarnSubstitutions.xml")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--nowarn", "IL2006,2007;2008;2009,2010,IL2011;IL2012,ThisWillBeIgnored")]
	[LogDoesNotContain ("warning")]
	public class CanDisableWarnings
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
