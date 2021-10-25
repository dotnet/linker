using System;

#nullable enable

namespace ILLink.Shared
{
	// The interface constraint on TValue also ensures that trying to instantiate
	// ILattice over a nullable type will produce a warning or error.
	// We could also add the notnull constraint for good measure.
	public interface ILattice<TValue> where TValue : IEquatable<TValue>
	{
		public TValue Top { get; }

		public TValue Meet (TValue left, TValue right);
	}
}