// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct BlockProxy : IEquatable<BlockProxy>
	{
		public readonly BasicBlock Block;

		public BlockProxy (BasicBlock block) => Block = block;

		public IEnumerable<BlockProxy> Predecessors {
			get {
				foreach (var predecessor in Block.Predecessors)
					yield return new BlockProxy (predecessor.Source);
			}
		}

		public bool Equals (BlockProxy other) => Block.Equals (other.Block);
	}

	public readonly struct ControlFlowGraphProxy : IControlFlowGraph<BlockProxy>
	{
		readonly ControlFlowGraph ControlFlowGraph;

		public ControlFlowGraphProxy (ControlFlowGraph cfg) => ControlFlowGraph = cfg;

		public IEnumerable<BlockProxy> Blocks {
			get {
				foreach (var block in ControlFlowGraph.Blocks)
					yield return new BlockProxy (block);
			}
		}

		public BlockProxy Entry => new BlockProxy (ControlFlowGraph.Blocks[0]);

		public IEnumerable<BlockProxy> GetPredecessors (BlockProxy block)
		{
			foreach (var predecessor in block.Block.Predecessors)
				yield return new (predecessor.Source);
		}
	}

	public class TrimDataFlowAnalysis
		: ForwardDataFlowAnalysis<
			LocalState<ValueSet<SingleValue>>,
			LocalStateLattice<ValueSet<SingleValue>, ValueSetLattice<SingleValue>>,
			BlockProxy,
			ControlFlowGraphProxy,
			TrimAnalysisVisitor
		>
	{
		readonly ControlFlowGraphProxy ControlFlowGraph;

		readonly LocalStateLattice<ValueSet<SingleValue>, ValueSetLattice<SingleValue>> Lattice;

		TrimAnalysisVisitor? Visitor;

		readonly OperationBlockAnalysisContext Context;

		public TrimDataFlowAnalysis (OperationBlockAnalysisContext context, ControlFlowGraph cfg)
		{
			ControlFlowGraph = new ControlFlowGraphProxy (cfg);
			Lattice = new (new ValueSetLattice<SingleValue> ());
			Visitor = null;
			Context = context;
		}

		public IEnumerable<TrimAnalysisPattern> GetTrimAnalysisPatterns ()
		{
			if (Visitor != null)
				return Visitor.TrimAnalysisPatterns;

			Visitor = new TrimAnalysisVisitor (Lattice, Context);
			Fixpoint (ControlFlowGraph, Lattice, Visitor);
			return Visitor.TrimAnalysisPatterns;
		}
	}
}