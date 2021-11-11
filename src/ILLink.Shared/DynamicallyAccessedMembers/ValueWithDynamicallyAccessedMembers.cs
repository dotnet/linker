// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace ILLink.Shared
{
	public abstract class ValueWithDynamicallyAccessedMembers : SingleValue
	{
		public abstract DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
	}
}