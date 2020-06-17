using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[SkipKeptItemsValidation]
	[SetupLinkerCoreAction ("skip")]
	[SetupLinkerArgument ("--verbose")]
	[SetupLinkerArgument ("--generate-warning-suppressions", new[] { "WarningSuppressions.cs" })]
	public class CanGenerateWarningSuppressionFile
	{
		public static void Main ()
		{
			var triggerWarnings = new Warnings ();
			triggerWarnings.Warning1 ();
			var getProperty = triggerWarnings.Warning2;
			var triggerWarningsFromNestedType = new Warnings.NestedType ();
			triggerWarningsFromNestedType.Warning3 ();
			var list = new List<int> ();
			triggerWarningsFromNestedType.Warning4 (ref list);
		}
	}

	class Warnings
	{
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (CanGenerateWarningSuppressionFile);
		}

		public void Warning1 ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public int Warning2 {
			get {
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}

		public class NestedType
		{
			public void Warning3 ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}

			public void Warning4<T> (ref List<T> p)
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}
}
