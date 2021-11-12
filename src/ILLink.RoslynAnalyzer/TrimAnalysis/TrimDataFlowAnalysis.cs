// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	// Blocks should be usable as keys of a dictionary.
	// The record equality implementation will check for reference equality
	// on the underlying BasicBlock, so uses of this class should not expect
	// any kind of value equality for different block instances. In practice
	// this should be fine as long as we consistently use block instances from
	// a single ControlFlowGraph.
	public readonly record struct BlockProxy (BasicBlock Block)
	{
		public IEnumerable<BlockProxy> Predecessors {
			get {
				foreach (var predecessor in Block.Predecessors)
					yield return new BlockProxy (predecessor.Source);
			}
		}
	}

	public readonly record struct ControlFlowGraphProxy (ControlFlowGraph ControlFlowGraph) : IControlFlowGraph<BlockProxy>
	{
		public IEnumerable<BlockProxy> Blocks {
			get {
				foreach (var block in ControlFlowGraph.Blocks)
					yield return new BlockProxy (block);
			}
		}

		public BlockProxy Entry => new BlockProxy (ControlFlowGraph.Blocks[0]);

		public IEnumerable<BlockProxy> GetPredecessors (BlockProxy block) => block.Predecessors;
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