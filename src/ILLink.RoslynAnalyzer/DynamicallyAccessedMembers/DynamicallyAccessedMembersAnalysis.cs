// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using ILLink.Shared;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public readonly struct BlockWrapper : IEquatable<BlockWrapper>
	{
		public readonly BasicBlock Block;

		public BlockWrapper (BasicBlock block) => Block = block;

		public IEnumerable<BlockWrapper> Predecessors {
			get {
				foreach (var predecessor in Block.Predecessors)
					yield return new BlockWrapper (predecessor.Source);
			}
		}

		public bool Equals (BlockWrapper other) => Block.Equals (other.Block);
	}

	public readonly struct ControlFlowGraphWrapper : IControlFlowGraph<BlockWrapper>
	{
		readonly ControlFlowGraph ControlFlowGraph;

		public ControlFlowGraphWrapper (ControlFlowGraph cfg) => ControlFlowGraph = cfg;

		public IEnumerable<BlockWrapper> Blocks {
			get {
				foreach (var block in ControlFlowGraph.Blocks)
					yield return new BlockWrapper (block);
			}
		}

		public BlockWrapper Entry => new BlockWrapper (ControlFlowGraph.Blocks[0]);

		public IEnumerable<BlockWrapper> GetPredecessors (BlockWrapper block)
		{
			foreach (var predecessor in block.Block.Predecessors)
				yield return new (predecessor.Source);
		}
	}

	public class DynamicallyAccessedMembersAnalysis
		: ForwardDataFlowAnalysis<
			LocalState<HashSetWrapper<SingleValue>>,
			LocalStateLattice<HashSetWrapper<SingleValue>, HashSetLattice<SingleValue>>,
			BlockWrapper,
			ControlFlowGraphWrapper,
			DynamicallyAccessedMembersVisitor
		>
	{
		readonly ControlFlowGraphWrapper ControlFlowGraph;

		readonly LocalStateLattice<HashSetWrapper<SingleValue>, HashSetLattice<SingleValue>> Lattice;

		DynamicallyAccessedMembersVisitor? Visitor;

		readonly OperationBlockAnalysisContext Context;

		public DynamicallyAccessedMembersAnalysis (OperationBlockAnalysisContext context, ControlFlowGraph cfg)
		{
			ControlFlowGraph = new ControlFlowGraphWrapper (cfg);
			Lattice = new (new HashSetLattice<SingleValue> ());
			Visitor = null;
			Context = context;
		}

		public IEnumerable<ReflectionAccessPattern> GetResults ()
		{
			if (Visitor != null)
				return Visitor.AccessPatterns;

			Visitor = new DynamicallyAccessedMembersVisitor (Context, Lattice);
			Fixpoint (ControlFlowGraph, Lattice, Visitor);
			return Visitor.AccessPatterns;
		}
	}
}