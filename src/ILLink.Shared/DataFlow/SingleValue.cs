// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared.DataFlow
{
	// This is a sum type over the various kinds of values we track:
	// - dynamicallyaccessedmembertypes-annotated locations (types or strings)
	// - known typeof values and similar
	// - known strings
	// - known integers

	public abstract record SingleValue;

	// SingleValues that are mutable should implement IDeepCopyValue to be copied in Lattice.Meet()
	public interface IDeepCopyValue<TSingleValue>
	{
		public TSingleValue DeepCopy ();
	}
}