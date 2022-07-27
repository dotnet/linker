// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;


namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	public class DetectRedundantSuppressionsInMembersAndTypes
	{
		public static void Main ()
		{
			RedundantSuppressionOnType.Test ();
			RedundantSuppressionOnMethod.Test ();
			RedundantSuppressionOnLocalMethod.Test ();
			RedundantSuppressionOnNestedType.Test ();
			RedundantSuppressionOnProperty.Test ();
			MultipleRedundantSuppressions.Test ();
			RedundantAndUsedSuppressions.Test ();
			DoNotReportNonLinkerSuppressions.Test ();
			DoNotReportSuppressionsOnMethodsConvertedToThrow.Test ();
			SuppressRedundantSuppressionWarning.Test ();
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsInMembersAndTypes);
		}

		public static string TrimmerCompatibleMethod ()
		{
			return "test";
		}

		[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2071")]
		public class RedundantSuppressionOnType
		{
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnMethod
		{
			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantSuppressionOnLocalMethod
		{
			public static void Test ()
			{
				[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2071")]
				void LocalMethod ()
				{
					TrimmerCompatibleMethod ();
				}

				LocalMethod ();
			}
		}

		public class RedundantSuppressionOnNestedType
		{
			public static void Test ()
			{
				NestedType.TrimmerCompatibleMethod ();
			}

			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public class NestedType
			{
				public static void TrimmerCompatibleMethod ()
				{
					DetectRedundantSuppressionsInMembersAndTypes.TrimmerCompatibleMethod ();
				}
			}
		}

		public class RedundantSuppressionOnProperty
		{
			public static void Test ()
			{
				var property = TrimmerCompatibleProperty;
			}

			public static string TrimmerCompatibleProperty {
				[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
				[UnconditionalSuppressMessage ("Test", "IL2071")]
				get {
					return TrimmerCompatibleMethod ();
				}
			}
		}

		[ExpectedWarning ("IL2121", "IL2072", ProducedBy = ProducedBy.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2072")]
		[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
		[UnconditionalSuppressMessage ("Test", "IL2071")]
		public class MultipleRedundantSuppressions
		{
			[ExpectedWarning ("IL2121", "IL2072", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class RedundantAndUsedSuppressions
		{
			[ExpectedWarning ("IL2121", "IL2071", ProducedBy = ProducedBy.Trimmer)]
			[UnconditionalSuppressMessage ("Test", "IL2071")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}

		public class DoNotReportNonLinkerSuppressions
		{
			[UnconditionalSuppressMessage ("Test", "IL3052")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}

		public class DoNotReportSuppressionsOnMethodsConvertedToThrow
		{
			// The tool is unable to determine whether a suppression is redundant when it is placed on a method with unreachable body.
			// Currently suppressions on methods with unreachable bodies should never be reported as redundant.
			// https://github.com/dotnet/linker/issues/2920
			public static void Test ()
			{
				UsedToMarkMethod (null);
			}

			static void UsedToMarkMethod (TypeWithMethodConvertedToThrow t)
			{
				t.MethodConvertedToThrow ();
			}

			class TypeWithMethodConvertedToThrow
			{
				// The suppression is redundant, but it should not be reported.
				[UnconditionalSuppressMessage ("Test", "IL2072")]
				public void MethodConvertedToThrow ()
				{
					TrimmerCompatibleMethod ();
				}
			}
		}

		public class SuppressRedundantSuppressionWarning
		{
			[UnconditionalSuppressMessage ("Test", "IL2121")]
			[UnconditionalSuppressMessage ("Test", "IL2072")]
			public static void Test ()
			{
				TrimmerCompatibleMethod ();
			}
		}
	}
}
