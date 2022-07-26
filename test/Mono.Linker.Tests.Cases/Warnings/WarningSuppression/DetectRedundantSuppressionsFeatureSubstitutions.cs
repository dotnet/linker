﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[SetupLinkerSubstitutionFile ("DetectRedundantSuppressionsFeatureSubstitutions.xml")]
	[SetupLinkerArgument ("--feature", "Feature", "false")]
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsFeatureSubstitutions
	{
		// https://github.com/dotnet/linker/issues/2920
		public static void Main ()
		{
			ReportRedundantSuppressionWhenTrimmerIncompatibleCodeDisabled.Test ();
			DoNotReportUsefulSuppressionWhenTrimmerIncompatibleCodeEnabled.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsFeatureSubstitutions);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		public static bool IsFeatureEnabled {
			get => throw new NotImplementedException ();
		}

		class ReportRedundantSuppressionWhenTrimmerIncompatibleCodeDisabled
		{
			[ExpectedWarning ("IL2121", "IL2072")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				if (IsFeatureEnabled) {
					Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				} else {
					TrimmerCompatibleMethod ();
				}
			}
		}

		class DoNotReportUsefulSuppressionWhenTrimmerIncompatibleCodeEnabled
		{
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				if (!IsFeatureEnabled) {
					Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				} else {
					TrimmerCompatibleMethod ();
				}
			}
		}
	}
}
