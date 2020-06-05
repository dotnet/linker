﻿using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

[assembly: UnconditionalSuppressMessage ("Test", "IL2006:Suppress unrecognized reflection pattern warnings in this assembly")]

namespace Mono.Linker.Tests.Cases.WarningSuppression
{
	[SkipKeptItemsValidation]
	[LogDoesNotContain ("TriggerUnrecognizedPattern()")]
	public class SuppressWarningsInAssembly
	{
		public static void Main ()
		{
			Expression.Call (TriggerUnrecognizedPattern (), "", Type.EmptyTypes);
		}

		public static Type TriggerUnrecognizedPattern ()
		{
			return typeof (SuppressWarningsInAssembly);
		}
	}
}
