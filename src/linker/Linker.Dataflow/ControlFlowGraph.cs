// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared.DataFlow;
using Mono.Cecil.Cil;
using System.Diagnostics.CodeAnalysis;

using Predecessor = ILLink.Shared.DataFlow.IControlFlowGraph<
	Mono.Linker.Dataflow.BasicBlock, 
	Mono.Linker.Dataflow.Region
>.Predecessor;

namespace Mono.Linker.Dataflow
{
	public record struct Region : IRegion<Region>
	{
		public RegionKind Kind => throw new NotImplementedException ();
	}

	public struct BasicBlock : IEquatable<BasicBlock>
	{
		public List<Instruction> Instructions;

		public BasicBlock ()
		{
			Instructions = new List<Instruction> ();
		}

		public bool Equals (BasicBlock other)
		{
			return Instructions == other.Instructions;
		}

		public override string ToString ()
		{
			if (Instructions.Count == 0) return "Empty";

			return $"[IL_{Instructions[0].Offset:X4}, IL_{Instructions[^1].Offset:X4}]";
		}
	}

	public struct ControlFlowGraph : IControlFlowGraph<BasicBlock, Region>
	{
		private const int firstBlockId = int.MinValue;
		private const int lastBlockId = int.MaxValue;
		private BasicBlock first;
		private readonly Dictionary<int, BasicBlock> basicBlocks;
		private readonly Dictionary<BasicBlock, HashSet<BasicBlock>> edges;

		public override string ToString ()
		{
			if (basicBlocks.Count == 0) return "";

			var orderedBlocks = basicBlocks.OrderBy (o => o.Key).Select(o => o.Value).ToList();

			var blockIndices = new Dictionary<BasicBlock, int> ();

			foreach (var block in orderedBlocks) {
				blockIndices.Add (block, blockIndices.Count);
			}

			var nodes = new List<string> ();

			foreach (var block in orderedBlocks) {
				var predecessors = GetPredecessors (block).Select (o => blockIndices[o.Block]).Order();
				nodes.Add($"Id: {blockIndices[block]}, Range: {block}, Predecessors: [{string.Join (",", predecessors)}]");
			}
			return string.Join (" | ", nodes);
		}

		public IEnumerable<BasicBlock> Blocks => basicBlocks.Values;

		public BasicBlock Entry => first;

		public ControlFlowGraph (MethodBody methodBody)
		{
			basicBlocks = new Dictionary<int, BasicBlock> ();
			edges = SplitMethodBody (methodBody);
		}

		public IEnumerable<Predecessor> GetPredecessors (BasicBlock block)
		{
			if(!edges.TryGetValue(block, out var preceedingBlocks)) {
				throw new Exception ("change this exception later");
			}
			foreach (var prevBlock in preceedingBlocks) {
				yield return new Predecessor (prevBlock, ImmutableArray<Region>.Empty);
			}
		}

		public bool TryGetEnclosingTryOrCatchOrFilter (BasicBlock block, [NotNullWhen (true)] out Region tryOrCatchOrFilterRegion)
		{
			throw new NotImplementedException ();
		}

		public bool TryGetEnclosingTryOrCatchOrFilter (Region region, [NotNullWhen (true)] out Region tryOrCatchOrFilterRegion)
		{
			throw new NotImplementedException ();
		}

		public bool TryGetEnclosingFinally (BasicBlock block, [NotNullWhen (true)] out Region region)
		{
			throw new NotImplementedException ();
		}

		public Region GetCorrespondingTry (Region cathOrFilterOrFinallyRegion)
		{
			throw new NotImplementedException ();
		}

		public IEnumerable<Region> GetPreviousFilters (Region catchOrFilterRegion)
		{
			throw new NotImplementedException ();
		}

		public bool HasFilter (Region catchRegion)
		{
			throw new NotImplementedException ();
		}

		public BasicBlock FirstBlock (Region region)
		{
			throw new NotImplementedException ();
		}

		public BasicBlock LastBlock (Region region)
		{
			throw new NotImplementedException ();
		}

		private Dictionary<BasicBlock, HashSet<BasicBlock>> SplitMethodBody (MethodBody methodBody)
		{
			ConstructBasicBlocks (methodBody);
			var cfg = new Dictionary<BasicBlock, HashSet<BasicBlock>> ();

			first = new BasicBlock ();
			basicBlocks.Add (firstBlockId, first);
			basicBlocks.Add (lastBlockId, new BasicBlock ());


			foreach (var (_, basicBlock) in basicBlocks) {
				cfg.Add (basicBlock, new HashSet<BasicBlock> ());
			}

			foreach (var (_, basicBlock) in basicBlocks.Where (o => o.Value.Instructions.Count != 0).OrderBy(o => o.Key)) {

				var firstInstruction = basicBlock.Instructions[0];
				if(firstInstruction.Previous == null) {
					cfg[basicBlock].Add (basicBlocks[firstBlockId]);
				}

				var lastInstruction = basicBlock.Instructions[^1];

				// Handle conditional branches

				if (lastInstruction.OpCode.IsControlFlowInstruction()) {
					var jumpTargets = lastInstruction.GetJumpTargets ();

					foreach (var jumpTarget in jumpTargets) {
						cfg[basicBlocks[jumpTarget.Offset]].Add (basicBlock);
					}

					if (lastInstruction.OpCode.FlowControl == FlowControl.Cond_Branch && lastInstruction.Next != null) {
						cfg[basicBlocks[lastInstruction.Next.Offset]].Add (basicBlock);
					}
				}
				// Handle last block predecessors
				else if (lastInstruction.OpCode.FlowControl == FlowControl.Return) {
					cfg[basicBlocks[lastBlockId]].Add (basicBlock);
				} 
				// Handle fall through
				else if (lastInstruction.OpCode.FlowControl == FlowControl.Next && lastInstruction.Next != null) {
					cfg[basicBlocks[lastInstruction.Next.Offset]].Add (basicBlock);
				}
			}

			return cfg;
		}

		private void ConstructBasicBlocks (MethodBody methodBody)
		{
			var currentBlock = new BasicBlock ();
			var leaders = methodBody.GetInitialBasicBlockInstructions ();

			foreach (Instruction operation in methodBody.Instructions) {
				if (leaders.Contains (operation.Offset)) {
					currentBlock = new BasicBlock ();
					basicBlocks.Add (operation.Offset, currentBlock);
				}

				currentBlock.Instructions.Add (operation);
			}
		}
	}
}
