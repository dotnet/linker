﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial record ArrayValue : IDeepCopyValue<SingleValue>
	{
		public readonly Dictionary<int, MultiValue> IndexValues;

		public ArrayValue (SingleValue size, MultiValue[] elements)
		{
			Size = size;
			IndexValues = new Dictionary<int, MultiValue> (elements.Length);
			for (int i = 0; i < elements.Length; i++) {
				IndexValues.Add (i, elements[i]);
			}
		}

		public partial bool TryGetValueByIndex (int index, out MultiValue value)
		{
			if (IndexValues.TryGetValue (index, out value))
				return true;

			value = default;
			return false;
		}

		public override int GetHashCode ()
		{
			return HashUtils.Combine (GetType ().GetHashCode (), Size);
		}

		public bool Equals (ArrayValue? otherArr)
		{
			if (otherArr == null)
				return false;

			bool equals = Size.Equals (otherArr.Size);
			equals &= IndexValues.Count == otherArr.IndexValues.Count;
			if (!equals)
				return false;

			// If both sets T and O are the same size and "T intersect O" is empty, then T == O.
			HashSet<KeyValuePair<int, MultiValue>> thisValueSet = new (IndexValues);
			thisValueSet.ExceptWith (otherArr.IndexValues);
			return thisValueSet.Count == 0;
		}

		// Lattice Meet() is supposed to copy values, so we need to make a deep copy since ArrayValue is mutable through IndexValues
		public SingleValue DeepCopy ()
		{
			List<MultiValue> elements = new ();
			for (int i = 0; IndexValues.TryGetValue (i, out var value); i++) {
				elements.Add (value);
			}
			return new ArrayValue (Size, elements.ToArray ());
		}
	}
}
