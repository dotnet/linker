using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.WarningSuppression.Dependencies;

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/WarningMessageSuppression_Lib.cs" })]
	[KeptAssembly ("library.dll")]
	[LogDoesNotContain ("ILlinker: Unrecognized reflection pattern warning IL2006: The return value of method " +
		"'System.Type Mono.Linker.Tests.Cases.WarningSuppression.WarningMessageSuppression::TriggerUnrecognizedPattern()'")]
	[LogDoesNotContain ("ILlinker: Unrecognized reflection pattern warning IL2006: The return value of method " +
		"'System.Type Mono.Linker.Tests.Cases.WarningSuppression.Dependencies.WarningMessageSuppression_Lib::TriggerUnrecognizedPattern()'")]
	public class WarningMessageSuppression
	{
		public static void Main ()
		{
			var suppressWarningsInType = new SuppressWarningsInType ();
			suppressWarningsInType.Warning1 ();
			suppressWarningsInType.Warning2 ();

			var suppressWarningsInMembers = new SuppressWarningsInMembers ();
			suppressWarningsInMembers.Method ();
			int propertyThatTriggersWarning = suppressWarningsInMembers.Property;

			NestedType.TriggerWarning ();
			WarningMessageSuppression_Lib.Warning ();
		}

		[Kept]
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (WarningMessageSuppression);
		}

		[Kept]
		public class NestedType
		{
			[Kept]
			public static void TriggerWarning ()
			{
				Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}
	}

	[UnconditionalSuppressMessage ("Test", "IL2006:UnrecognizedReflectionPattern")]
	[Kept]
	[KeptMember (".ctor()")]
	[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
	public class SuppressWarningsInType
	{
		[Kept]
		public void Warning1 ()
		{
			Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		[Kept]
		public void Warning2 ()
		{
			Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}
	}

	[KeptMember (".ctor()")]
	public class SuppressWarningsInMembers
	{
		[UnconditionalSuppressMessage ("Test", "IL2006:UnrecognizedReflectionPattern")]
		[Kept]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		public void Method ()
		{
			Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);

			void LocalFunction ()
			{
				Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
			}
		}

		[UnconditionalSuppressMessage ("Test", "IL2006:UnrecognizedReflectionPattern")]
		[Kept]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		public int Property {
			[Kept]
			get {
				Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}
	}
}