// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	// A lattice over stacks where the stored values are also from a lattice.
	public readonly struct StackLattice<TValue, TValueLattice> : ILattice<StructStack<TValue>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILatticeWithUnknownValue<TValue>
	{
		public readonly TValueLattice ValueLattice;

		public StructStack<TValue> Top { get; }

		public StackLattice (TValueLattice valueLattice)
		{
			ValueLattice = valueLattice;
			Top = new StructStack<TValue> ();
		}

		public StructStack<TValue> Meet (StructStack<TValue> left, StructStack<TValue> right)
		{
			// Meet(value, Top) = value
			if (left.Equals (Top)) return new StructStack<TValue> (new StructStack<TValue> (right));

			if (right.Equals (Top)) return new StructStack<TValue> (new StructStack<TValue> (left));

			if (left.Count != right.Count) {
				// Force stacks to be of equal size to avoid crashes.
				// Analysis of this method will be incorrect.
				while (left.Count < right.Count)
					left.Push (ValueLattice.UnknownValue);

				while (right.Count < left.Count)
					right.Push (ValueLattice.UnknownValue);
			}

			StructStack<TValue> newStack = new StructStack<TValue> (left.Count);
			IEnumerator<TValue> aEnum = left.GetEnumerator ();
			IEnumerator<TValue> bEnum = right.GetEnumerator ();
			while (aEnum.MoveNext () && bEnum.MoveNext ()) {
				newStack.Push (ValueLattice.Meet (aEnum.Current, bEnum.Current));
			}

			// The new stack is reversed. Use the copy constructor to reverse it back
			return new StructStack<TValue> (newStack);
		}
	}
}
