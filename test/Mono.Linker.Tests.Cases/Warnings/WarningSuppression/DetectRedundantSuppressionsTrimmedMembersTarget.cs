// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

[assembly: UnconditionalSuppressMessage ("Test", "IL2071", Scope = "type", Target = "T:Mono.Linker.Tests.Cases.Warnings.WarningSuppression.UnusedTypeWithRedundantSuppression")]

namespace Mono.Linker.Tests.Cases.Warnings.WarningSuppression
{
	[ExpectedNoWarnings]
	[SkipKeptItemsValidation]
	class DetectRedundantSuppressionsTrimmedMembersTarget
	{
		[ExpectedWarning ("IL2072")]
		static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (DetectRedundantSuppressionsTrimmedMembersTarget);
		}
	}

	class UnusedTypeWithRedundantSuppression
	{

	}
}
