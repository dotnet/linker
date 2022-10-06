// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	// A lattice over stacks where the stored values are also from a lattice.
	public readonly struct StackLattice<TValue, TValueLattice> : ILattice<ValueStack<TValue>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly TValueLattice ValueLattice;

		public ValueStack<TValue> Top { get; }

		public StackLattice (TValueLattice valueLattice)
		{
			ValueLattice = valueLattice;
			Top = new ValueStack<TValue> ();
		}

		public ValueStack<TValue> Meet (ValueStack<TValue> left, ValueStack<TValue> right)
		{
			// Meet(value, Top) = value
			if (left.Equals (Top)) return new ValueStack<TValue> (new ValueStack<TValue> (right));

			if (right.Equals (Top)) return new ValueStack<TValue> (new ValueStack<TValue> (left));

			if (left.Count != right.Count) {
				throw new InvalidOperationException ("Stacks have different sizes");
			}

			ValueStack<TValue> newStack = new ValueStack<TValue> (left.Count);
			IEnumerator<TValue> aEnum = left.GetEnumerator ();
			IEnumerator<TValue> bEnum = right.GetEnumerator ();
			while (aEnum.MoveNext () && bEnum.MoveNext ()) {
				newStack.Push (ValueLattice.Meet (aEnum.Current, bEnum.Current));
			}

			// The new stack is reversed. Use the copy constructor to reverse it back
			return new ValueStack<TValue> (newStack);
		}
	}
}
