using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
[module: UnconditionalSuppressMessage ("Test", "IL2071:Redundant suppression, warning is not issued in this assembly")]


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInModule
	{
		public static void Main ()
		{
			TrimmerCompatibleMethod ();
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}
	}
}
