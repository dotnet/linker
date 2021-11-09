// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	// The interface constraint on TValue also ensures that trying to instantiate
	// ILattice over a nullable type will produce a warning or error.
	public interface ILattice<TValue> where TValue : IEquatable<TValue>
	{
		public TValue Top { get; }

		public TValue Meet (TValue left, TValue right);
	}
}