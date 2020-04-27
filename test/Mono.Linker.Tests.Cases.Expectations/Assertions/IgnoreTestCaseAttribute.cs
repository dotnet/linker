﻿using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class)]
	public class IgnoreTestCaseAttribute : Attribute
	{

		public IgnoreTestCaseAttribute (string reason)
		{
			if (reason == null)
				throw new ArgumentNullException (nameof (reason));
		}
	}
}