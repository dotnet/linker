// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;

namespace Mono.Linker.Dataflow
{
	public class BlockDataFlowState<TValue, TValueLattice>
		: IDataFlowState<BlockState<TValue>, BlockStateLattice<TValue, TValueLattice>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILatticeWithUnknownValue<TValue>
	{
		BlockState<TValue> current;
		public BlockState<TValue> Current {
			get => current;
			set => current = value;
		}

		public Box<BlockState<TValue>>? Exception { get; set; }

		public BlockStateLattice<TValue, TValueLattice> Lattice { get; init; }

		public void Set (LocalKey key, TValue value)
		{
			current.Set (key, value);
			if (Exception != null)
				Exception.Value = Lattice.Meet (Exception.Value, current);
		}

		public TValue Get (LocalKey key) => current.Get (key);

		public TValue Pop () => current.Pop ();

		public TValue Pop (int count) => current.Pop (count);

		public void Push (TValue value) => current.Push (value);
	}
}
