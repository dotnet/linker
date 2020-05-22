using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
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
		}

		[Kept]
		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (WarningMessageSuppression);
		}

		[Kept]
		[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
		[UnconditionalSuppressMessage ("Test", "IL2006:UnrecognizedReflectionPattern")]
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

		[Kept]
		public int Property {
			[Kept]
			[UnconditionalSuppressMessage ("Test", "IL2006:UnrecognizedReflectionPattern")]
			[KeptAttributeAttribute (typeof (UnconditionalSuppressMessageAttribute))]
			get {
				Expression.Call (WarningMessageSuppression.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
				return 0;
			}
		}
	}
}