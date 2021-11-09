// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	// Similar to ValueNode, and a candidate for future code sharing.
	// This is a sum type over the various kinds of values we track:
	// - dynamicallyaccessedmembertypes-annotated locations (types or strings)
	// - known typeof values and similar
	// - known strings
	// - known integers

	// The implementation is taken from the generated code for records.
	// We could just make it a record if we weren't targeting netstandard2.0.
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