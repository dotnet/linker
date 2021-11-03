// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	// TODO: merge with ValueNode
	// logically this is a sum type over:
	// - dynamicallyaccessedmembertypes-annotated locations (types or strings)
	// - known typeof values and similar
	// - known strings
	// - known integers
	// and anything else we track
	public abstract class SingleValue : IEquatable<SingleValue>
	{
		public virtual bool Equals (SingleValue? other)
		{
			return this == other || (other != null && EqualityContract == other.EqualityContract);
		}

		public override int GetHashCode () => EqualityContract.GetHashCode ();

		protected virtual Type EqualityContract => typeof (SingleValue);
	}
}