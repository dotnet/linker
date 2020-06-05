﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

[module: UnconditionalSuppressMessage ("Test", "IL2006", Scope = "type", Target = "T:Mono.Linker.Tests.Cases.WarningSuppression.WarningsInType")]
[module: UnconditionalSuppressMessage ("Test", "IL2006", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.WarningSuppression.WarningsInMembers.Method")]
[module: UnconditionalSuppressMessage ("Test", "IL2006", Scope = "member", Target = "M:Mono.Linker.Tests.Cases.WarningSuppression.WarningsInMembers.get_Property")]

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsInMembersAndTypesUsingTarget
	{
		public static void Main ()
		{
			var warningsInType = new WarningsInType ();
			warningsInType.Warning1 ();
			warningsInType.Warning2 ();
			var warningInNestedType = new WarningsInType.NestedType ();
			warningInNestedType.Warning3 ();

			var warningsInMembers = new WarningsInMembers ();
			warningsInMembers.Method ();
			int propertyThatTriggersWarning = warningsInMembers.Property;
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (SuppressWarningsInMembersAndTypesUsingTarget);
		}

		public class NestedType
		{
			public static void Warning ()
			{
				Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}

	public class WarningsInType
	{
		public void Warning1 ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public void Warning2 ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public class NestedType
		{
			public void Warning3 ()
			{
				void Warning4 ()
				{
					Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				}

				SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern ();
				Warning4 ();
			}
		}
	}

	public class WarningsInMembers
	{
		public void Method ()
		{
			Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public int Property {
			get {
				Expression.Call (SuppressWarningsInMembersAndTypesUsingTarget.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}
	}
}
