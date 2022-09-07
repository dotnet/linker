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
using Mono.Collections.Generic;

namespace Mono.Linker.Dataflow
{
	public record struct Region : IRegion<Region>
	{
		public RegionKind Kind => throw new NotImplementedException ();
	}

	public struct BasicBlock : IEquatable<BasicBlock>
	{
		private readonly int _id;

		private readonly Collection<Instruction> _methodInstructions;

		private readonly int _start;

		private readonly int _end;

		public BasicBlock (Collection<Instruction> methodInstructions,int id, int start, int end)
		{
			_methodInstructions = methodInstructions;
			_start = start;
			_end = end;
			_id = id;
		}

		public int Id => _id;

		public Instruction? FirstInstruction => _start >= 0 ? _methodInstructions[_start] : null;

		public Instruction? LastInstruction => _end >= 0 ? _methodInstructions[_end] : null;

		public bool Equals (BasicBlock other)
		{
			return _start == other._start && _end == other._end;
		}

		public override string ToString ()
		{
			if (_end == -1) return "Empty";

			return $"[IL_{_methodInstructions[_start].Offset:X4}, IL_{_methodInstructions[_end].Offset:X4}]";
		}
	}

	public struct ControlFlowGraph : IControlFlowGraph<BasicBlock, Region>
	{
		private readonly List<BasicBlock> _blocks;
		private readonly Dictionary<int, BasicBlock> firstInstructionToBlock;
		private readonly List<List<int>> edges;

		public override string ToString ()
		{
			if (firstInstructionToBlock.Count == 0) return "";

			var nodes = new List<string> ();

			foreach (var block in _blocks) {
				var predecessors = GetPredecessors (block).Select(o => o.Block.Id).ToList();
				nodes.Add($"Id: {block.Id}, Range: {block}, Predecessors: [{string.Join (",", predecessors)}]");
			}
			return string.Join (" | ", nodes);
		}

		public IEnumerable<BasicBlock> Blocks => firstInstructionToBlock.Values;

		public BasicBlock Entry => _blocks[0];

		public ControlFlowGraph (MethodBody methodBody)
		{
			firstInstructionToBlock = new Dictionary<int, BasicBlock> ();
			_blocks = new List<BasicBlock> ();
			edges = SplitMethodBody (methodBody);
		}

		public IEnumerable<Predecessor> GetPredecessors (BasicBlock block)
		{
			foreach (var prevBlockId in edges[block.Id]) {
				yield return new Predecessor (_blocks[prevBlockId], ImmutableArray<Region>.Empty);
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

		private List<List<int>> SplitMethodBody (MethodBody methodBody)
		{
			ConstructBasicBlocks (methodBody);
			var cfg = new List<List<int>>(_blocks.Count);

			foreach (var _ in _blocks) {
				cfg.Add (new List<int> ());
			}

			//Add initial block connections
			cfg[1].Add (0);

			foreach (var basicBlock in _blocks) {

				if(basicBlock.LastInstruction == null) {
					continue;
				}

				// Handle conditional branches
				if (basicBlock.LastInstruction.OpCode.IsControlFlowInstruction()) {
					var jumpTargets = basicBlock.LastInstruction.GetJumpTargets ();

					foreach (var jumpTarget in jumpTargets) {
						var targetId = firstInstructionToBlock[jumpTarget.Offset].Id;
						cfg[targetId].Add (basicBlock.Id);
					}

					if (basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Cond_Branch && basicBlock.LastInstruction.Next != null) {
						var targetId = firstInstructionToBlock[basicBlock.LastInstruction.Next.Offset].Id;
						cfg[targetId].Add (basicBlock.Id);
					}
				}
				// Handle last block predecessors
				else if (basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Return) {
					cfg[_blocks.Count-1].Add (basicBlock.Id);
				} 
				// Handle fall through
				else if (basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Next && basicBlock.LastInstruction.Next != null) {
					var targetId = firstInstructionToBlock[basicBlock.LastInstruction.Next.Offset].Id;
					cfg[targetId].Add (basicBlock.Id);
				}
			}

			return cfg;
		}

		private void ConstructBasicBlocks (MethodBody methodBody)
		{
			int blockStart = 0;

			// Add extra first block
			AddBlock(methodBody.Instructions, -1, -1);
			
			var leaders = methodBody.GetInitialBasicBlockInstructions ();

			for (int i = 1; i < methodBody.Instructions.Count; i++)
			{
				if (leaders.Contains (methodBody.Instructions[i].Offset)){
					AddBlock(methodBody.Instructions, blockStart, i - 1);
					blockStart = i;
				}
			}

			AddBlock (methodBody.Instructions, blockStart, methodBody.Instructions.Count - 1);

			// Add extra last block
			AddBlock(methodBody.Instructions, -1, -1);
		}

		private void AddBlock (Collection<Instruction> instructions, int ilStart, int ilEnd)
		{
			var block = new BasicBlock (instructions, _blocks.Count, ilStart, ilEnd);
			_blocks.Add (block);

			if(ilStart >= 0) {
				firstInstructionToBlock.Add (instructions[ilStart].Offset, block);
			}
		}
	}
}
