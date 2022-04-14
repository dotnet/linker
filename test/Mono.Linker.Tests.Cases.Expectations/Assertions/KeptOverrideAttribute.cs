// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
	/// <Summary>
	/// Used to ensure that a method should keep an 'override' annotation for a method in the supplied base type
	/// Fails in tests if the method doesn't have the override method in the original or linked assembly
	/// </Summary>
	public class KeptOverrideAttribute : KeptAttribute
	{
		public Type TypeWithOverriddenMethodDeclaration;

		public KeptOverrideAttribute (Type typeWithOverriddenMethod)
		{
			if (typeWithOverriddenMethod == null)
				throw new ArgumentNullException (nameof (typeWithOverriddenMethod));
			TypeWithOverriddenMethodDeclaration = typeWithOverriddenMethod;
		}
	}
}