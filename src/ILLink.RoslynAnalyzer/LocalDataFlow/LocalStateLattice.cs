// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public readonly struct LocalKey : IEquatable<LocalKey>
	{
		readonly ILocalSymbol? Local;

		readonly CaptureId? CaptureId;

		public LocalKey (ILocalSymbol symbol) => (Local, CaptureId) = (symbol, null);

		public LocalKey (CaptureId captureId) => (Local, CaptureId) = (null, captureId);

		public bool Equals (LocalKey other)
		{
			return SymbolEqualityComparer.Default.Equals (Local, other.Local) &&
				(CaptureId?.Equals (other.CaptureId) ?? other.CaptureId == null);
		}
	}

	// Derived class purely to substitute a concrete LocalKey for TKey of DefaultValueDictionary
	// Is this worth it?
	public class LocalState<TValue> : DefaultValueDictionary<LocalKey, TValue>,
		IEquatable<LocalState<TValue>>
		where TValue : IEquatable<TValue>
	{
		public LocalState (TValue unknown) : base (unknown) { }

		public LocalState (DefaultValueDictionary<LocalKey, TValue> defaultValueDictionary)
			: base (defaultValueDictionary)
		{
		}

		public bool Equals (LocalState<TValue> other) => base.Equals (other);
	}

	// Wrapper struct purely to substitute a concrete LocalKey for TKey of DictionaryLattice
	// Is this worth it?
	public struct LocalStateLattice<TValue, TValueLattice> : ILattice<LocalState<TValue>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly DictionaryLattice<LocalKey, TValue, TValueLattice> Lattice;

		public LocalStateLattice (TValueLattice valueLattice) => Lattice = new DictionaryLattice<LocalKey, TValue, TValueLattice> (valueLattice);

		public LocalState<TValue> Top => new (Lattice.Top); // TODO: static

		public LocalState<TValue> Meet (LocalState<TValue> left, LocalState<TValue> right) => new (Lattice.Meet (left, right));
	}
}