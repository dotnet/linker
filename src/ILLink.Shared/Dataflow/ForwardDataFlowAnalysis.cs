// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	public abstract class ForwardDataFlowAnalysis<TValue, TLattice, TBlock, TControlFlowGraph, TTransfer>
		where TValue : class, IEquatable<TValue>
		where TLattice : ILattice<TValue>
		where TBlock : IEquatable<TBlock>
		where TControlFlowGraph : IControlFlowGraph<TBlock>
		where TTransfer : ITransfer<TBlock, TValue, TLattice>
	{
		// This just runs a dataflow algorithm until convergence. It doesn't cache any results,
		// allowing each particular kind of analysis to decide what is worth saving.
		public static void Fixpoint (TControlFlowGraph cfg, TLattice lattice, TTransfer transfer)
		{
			// Initialize output of each block to the Top value of the lattice
			DefaultValueDictionary<TBlock, TValue> blockOutput = new (lattice.Top);

			bool changed = true;
			while (changed) {
				changed = false;
				foreach (var block in cfg.Blocks) {
					if (block.Equals (cfg.Entry))
						continue;

					// Meet over predecessors to get the new value at the start of this block.
					TValue blockState = lattice.Top;
					foreach (var predecessor in cfg.GetPredecessors (block))
						blockState = lattice.Meet (blockState, blockOutput.Get (predecessor));

					// Apply transfer function to the input to compute the output state after this block.
					// This mutates the block state in place.
					transfer.Transfer (block, blockState);

					if (!blockOutput.Get (block).Equals (blockState))
						changed = true;

					blockOutput[block] = blockState;
				}
			}
		}
	}
}