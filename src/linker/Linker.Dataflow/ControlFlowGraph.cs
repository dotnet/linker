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
		private readonly int _id;
		
		private readonly MethodBody _methodBody;

		private readonly int _start;

		private readonly int _end;

		public BasicBlock (MethodBody methodBody, int id, int start, int end)
		{
			_methodBody = methodBody;
			_start = start;
			_end = end;
			_id = id;
		}

		public MethodBody MethodBody => _methodBody;

		public int Id => _id;

		public Instruction? FirstInstruction => _start >= 0 ? _methodBody.Instructions[_start] : null;

		public Instruction? LastInstruction => _end >= 0 ? _methodBody.Instructions[_end] : null;

		public IEnumerable<Instruction> GetInstructions ()
		{
			for (int i = _start; i <= _end; i++) {
				yield return _methodBody.Instructions[i];
			}
		}

		public bool Equals (BasicBlock other)
		{
			return _start == other._start && _end == other._end;
		}

		public override string ToString ()
		{
			if (_end == -1) return "Empty";

			return $"[IL_{_methodBody.Instructions[_start].Offset:X4}, IL_{_methodBody.Instructions[_end].Offset:X4}]";
		}
	}

	public struct ControlFlowGraph : IControlFlowGraph<BasicBlock, Region>
	{
		private readonly List<BasicBlock> _blocks;
		private readonly List<List<int>> edges;

		public override string ToString ()
		{
			if (_blocks.Count == 0) return "";

			var nodes = new List<string> ();

			foreach (var block in Blocks) {
				var predecessors = GetPredecessors (block).Select(o => o.Block.Id).ToList();
				nodes.Add($"Id: {block.Id}, Range: {block}, Predecessors: [{string.Join (",", predecessors)}]");
			}
			return string.Join (" | ", nodes);
		}
		
		public IEnumerable<BasicBlock> Blocks => _blocks;

		public BasicBlock Entry => _blocks[0];

		private ControlFlowGraph (List<BasicBlock> blocks, List<List<int>> edges)
		{
			_blocks = blocks;
			this.edges = edges;
		}

		public static bool TryCreate (MethodBody method, out ControlFlowGraph cfg)
		{
			cfg = default;
			if (CanCreateControlFlowGraph (method)) {
				cfg = Create (method);
				return true;
			}
			return false;
		}

		public static ControlFlowGraph Create (MethodBody method)
		{
			var firstInstructionToBlock = new Dictionary<int, BasicBlock> ();
			var blocks = ConstructBasicBlocks (method, firstInstructionToBlock);
			var edges = GetEdges (firstInstructionToBlock, blocks);
			
			return new ControlFlowGraph (blocks, edges);
		}

		private static bool CanCreateControlFlowGraph (MethodBody method)
		{
			return !method.HasExceptionHandlers;
		}

		public IEnumerable<Predecessor> GetPredecessors (BasicBlock block)
		{
			foreach (var prevBlockId in edges[block.Id]) {
				yield return new Predecessor (_blocks[prevBlockId], ImmutableArray<Region>.Empty);
			}
		}

		public bool TryGetEnclosingTryOrCatchOrFilter (BasicBlock block, [NotNullWhen (true)] out Region tryOrCatchOrFilterRegion)
		{
			return false;
		}

		public bool TryGetEnclosingTryOrCatchOrFilter (Region region, [NotNullWhen (true)] out Region tryOrCatchOrFilterRegion)
		{
			return false;
		}

		public bool TryGetEnclosingFinally (BasicBlock block, [NotNullWhen (true)] out Region region)
		{
			return false;
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

		public static List<List<int>> GetEdges (Dictionary<int, BasicBlock> firstInstructionToBlock, List<BasicBlock> blocks)
		{
			var edges = new List<List<int>>(blocks.Count);
			
			foreach (var _ in blocks) {
				edges.Add (new List<int> ());
			}

			//Add initial block connections
			edges[1].Add (0);

			foreach (var basicBlock in blocks) {

				if(basicBlock.LastInstruction == null) {
					continue;
				}

				// Handle conditional branches
				if (basicBlock.LastInstruction.OpCode.IsControlFlowInstruction()) {
					var jumpTargets = basicBlock.LastInstruction.GetJumpTargets ();

					foreach (var jumpTarget in jumpTargets) {
						var targetId = firstInstructionToBlock[jumpTarget.Offset].Id;
						edges[targetId].Add (basicBlock.Id);
					}

					if (basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Cond_Branch && basicBlock.LastInstruction.Next != null) {
						var targetId = firstInstructionToBlock[basicBlock.LastInstruction.Next.Offset].Id;
						edges[targetId].Add (basicBlock.Id);
					}
				}
				// Handle last block predecessors
				else if (basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Return) {
					edges[blocks.Count-1].Add (basicBlock.Id);
				} 
				// Handle fall through
				else if ((basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Next || basicBlock.LastInstruction.OpCode.FlowControl == FlowControl.Call) && basicBlock.LastInstruction.Next != null) {
					var targetId = firstInstructionToBlock[basicBlock.LastInstruction.Next.Offset].Id;
					edges[targetId].Add (basicBlock.Id);
				}
			}

			return edges;
		}

		private static List<BasicBlock> ConstructBasicBlocks (MethodBody methodBody, Dictionary<int, BasicBlock> firstInstructionToBlock)
		{
			var blocks = new List<BasicBlock> ();
			int blockStart = 0;

			// Add extra first block
			AddBlock(methodBody, blocks, firstInstructionToBlock, - 1, -1);
			
			var leaders = methodBody.GetInitialBasicBlockInstructions ();
			
			for (int i = 1; i < methodBody.Instructions.Count; i++)
			{
				if (leaders.Contains (methodBody.Instructions[i].Offset)){
					AddBlock(methodBody, blocks, firstInstructionToBlock, blockStart, i - 1);
					blockStart = i;
				}
			}

			AddBlock (methodBody, blocks, firstInstructionToBlock, blockStart, methodBody.Instructions.Count - 1);

			// Add extra last block
			AddBlock (methodBody, blocks, firstInstructionToBlock, -1, -1);

			return blocks;
		}

		private static void AddBlock (MethodBody methodBody, List<BasicBlock> blocks, Dictionary<int, BasicBlock> firstInstructionToBlock, int ilStart, int ilEnd)
		{
			var block = new BasicBlock (methodBody, blocks.Count, ilStart, ilEnd);
			blocks.Add (block);

			if(ilStart >= 0) {
				if (!firstInstructionToBlock.TryAdd (methodBody.Instructions[ilStart].Offset, block))
					firstInstructionToBlock[methodBody.Instructions[ilStart].Offset] = block;
			}
		}
	}
}
