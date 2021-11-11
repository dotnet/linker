// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILLink.Shared
{
	// A lattice over ValrueSets where the Meet operation is just set union.
	public struct ValueSetLattice<TValue> : ILattice<ValueSet<TValue>>
		where TValue : IEquatable<TValue>
	{
		public ValueSet<TValue> Top => default;

		public ValueSet<TValue> Meet (ValueSet<TValue> left, ValueSet<TValue> right)
		{
			if (left.Values == null)
				return right;
			if (right.Values == null)
				return left;

			var values = new HashSet<TValue> (left.Values);
			values.UnionWith (right.Values);
			return new ValueSet<TValue> (values);
		}
	}
}