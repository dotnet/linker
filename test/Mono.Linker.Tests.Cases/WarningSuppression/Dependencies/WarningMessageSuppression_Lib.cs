using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text;

namespace Mono.Linker.Tests.Cases.WarningSuppression.Dependencies
{
	[UnconditionalSuppressMessage ("Test", "IL2006", Scope = "module")]

	public class WarningMessageSuppression_Lib
	{
		public static void Warning ()
		{
			Expression.Call (WarningMessageSuppression_Lib.TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		private static Type TriggerUnrecognizedPattern ()
		{
			return typeof (WarningMessageSuppression_Lib);
		}
	}
}
