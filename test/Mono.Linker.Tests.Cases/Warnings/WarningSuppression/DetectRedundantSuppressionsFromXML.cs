using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SkipKeptItemsValidation]
	[ExpectedNoWarnings]
	[SetupLinkAttributesFile ("DetectRedundantSuppressionsFromXML.xml")]
	public class DetectRedundantSuppressionsFromXML
	{
		public static void Main ()
		{
			DetectRedundantSuppressions.Test ();
		}

		[ExpectedWarning ("IL2121", "IL2109", ProducedBy = ProducedBy.Trimmer)]
		public class DetectRedundantSuppressions
		{
			[ExpectedWarning ("IL2121", "IL2026", ProducedBy = ProducedBy.Trimmer)]
			public static void Test ()
			{
				DoNotTriggerWarning ();
			}

			class SuppressedOnType : DoNotTriggerWarningType { }

			static void DoNotTriggerWarning () { }

			class DoNotTriggerWarningType { }
		}
	}
}