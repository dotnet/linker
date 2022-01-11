using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis.FlowAnalysis;

using Predecessor = ILLink.Shared.DataFlow.IControlFlowGraph<
	ILLink.RoslynAnalyzer.DataFlow.BlockProxy,
	ILLink.RoslynAnalyzer.DataFlow.RegionProxy
>.Predecessor;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Blocks should be usable as keys of a dictionary.
	// The record equality implementation will check for reference equality
	// on the underlying BasicBlock, so uses of this class should not expect
	// any kind of value equality for different block instances. In practice
	// this should be fine as long as we consistently use block instances from
	// a single ControlFlowGraph.
	public readonly record struct BlockProxy (BasicBlock Block)
	{
		public override string ToString ()
		{
			return base.ToString () + $"[{Block.Ordinal}]";
		}
	}

	public readonly record struct RegionProxy (ControlFlowRegion Region) : IRegion<RegionProxy>
	{
		public RegionKind Kind => Region.Kind switch {
			ControlFlowRegionKind.Try => RegionKind.Try,
			ControlFlowRegionKind.Catch => RegionKind.Catch,
			ControlFlowRegionKind.Finally => RegionKind.Finally,
			_ => throw new InvalidOperationException ()
		};
	}

	public readonly record struct ControlFlowGraphProxy (ControlFlowGraph ControlFlowGraph) : IControlFlowGraph<BlockProxy, RegionProxy>
	{
		public IEnumerable<BlockProxy> Blocks {
			get {
				foreach (var block in ControlFlowGraph.Blocks)
					yield return new BlockProxy (block);
			}
		}

		public BlockProxy Entry => new BlockProxy (ControlFlowGraph.Blocks[0]);

		// This is implemented by getting predecessors of the underlying Roslyn BasicBlock.
		// This is fine as long as the blocks come from the correct control-flow graph.
		public IEnumerable<Predecessor> GetPredecessors (BlockProxy block)
		{
			foreach (var predecessor in block.Block.Predecessors) {
				if (predecessor.FinallyRegions.IsEmpty) {
					yield return new Predecessor (new BlockProxy (predecessor.Source), ImmutableArray<RegionProxy>.Empty);
					continue;
				}
				var finallyRegions = ImmutableArray.CreateBuilder<RegionProxy> ();
				foreach (var region in predecessor.FinallyRegions) {
					if (region == null)
						throw new InvalidOperationException ();
					finallyRegions.Add (new RegionProxy (region));
				}
				yield return new Predecessor (new BlockProxy (predecessor.Source), finallyRegions.ToImmutable ());
			}
		}

		public bool TryGetEnclosingExceptionRegion (BlockProxy block, out RegionProxy exceptionRegion)
		{
			exceptionRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			while (region != null) {
				switch (region.Kind) {
				case ControlFlowRegionKind.Try:
				case ControlFlowRegionKind.Catch:
				case ControlFlowRegionKind.Finally:
					exceptionRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingTry (BlockProxy block, out RegionProxy tryRegion)
		{
			tryRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			while (region != null) {
				// This will prevent finding the TryAndCatch inside of a Try of a TryAndFinally region.
				// It will also prevent finding an enclosing Try if we are inside of a Catch.
				if (region.Kind == ControlFlowRegionKind.Catch)
					return false;
				if (region.Kind == ControlFlowRegionKind.Try) {
					tryRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingCatch (BlockProxy block, out RegionProxy catchRegion)
		{
			catchRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			while (region != null) {
				if (region.Kind == ControlFlowRegionKind.Catch) {
					catchRegion = new RegionProxy (region);
					return true;
				}
				if (region.Kind == ControlFlowRegionKind.Try)
					return false;
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingFinally (BlockProxy block, out RegionProxy catchRegion)
		{
			catchRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			while (region != null) {
				if (region.Kind == ControlFlowRegionKind.Finally) {
					catchRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingTryOrCatch (RegionProxy regionProxy, out RegionProxy tryOrCatchRegion)
		{
			tryOrCatchRegion = default;
			ControlFlowRegion? region = regionProxy.Region.EnclosingRegion;
			while (region != null) {
				if (region.Kind is ControlFlowRegionKind.Try or ControlFlowRegionKind.Catch) {
					tryOrCatchRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public bool TryGetEnclosingTryOrCatch (BlockProxy block, out RegionProxy tryOrCatchRegion)
		{
			tryOrCatchRegion = default;
			ControlFlowRegion? region = block.Block.EnclosingRegion;
			while (region != null) {
				if (region.Kind is ControlFlowRegionKind.Try or ControlFlowRegionKind.Catch) {
					tryOrCatchRegion = new RegionProxy (region);
					return true;
				}
				region = region.EnclosingRegion;
			}
			return false;
		}

		public RegionProxy GetCorrespondingTry (RegionProxy catchOrFinallyRegion)
		{
			if (catchOrFinallyRegion.Region.Kind is not (ControlFlowRegionKind.Finally or ControlFlowRegionKind.Catch))
				throw new ArgumentException (nameof (catchOrFinallyRegion));

			foreach (var nested in catchOrFinallyRegion.Region.EnclosingRegion!.NestedRegions) {
				// Note that for try+catch+finally, the try corresponding to the finally will not be the same as
				// the try corresponding to the catch, because Roslyn represents this region hierarchy the same as
				// a try+catch nested inside the try block of a try+finally (see comments in GetCorrespondingCatch).
				if (nested.Kind == ControlFlowRegionKind.Try)
					return new (nested);
			}
			throw new InvalidOperationException ();
		}

		public IEnumerable<RegionProxy> GetCorrespondingCatch (RegionProxy finallyRegion)
		{
			if (finallyRegion.Region.Kind != ControlFlowRegionKind.Finally)
				throw new ArgumentException (nameof (finallyRegion));

			foreach (var nested in finallyRegion.Region.EnclosingRegion!.NestedRegions) {
				// Note that try+catch+finally is represented as a TryAndFinally region whose Try
				// has a TryAndCatch region:
				// 
				// TryAndFinally
				//   Try
				//     TryAndCatch
				//       Try
				//       Catch
				//   Finally
				//
				// So we must go down into the inner TryAndCatch to see the corresponding catch region.
				// However, this means that a try { try {} catch {} } finally {} will report that the
				// inner catch corresponds to the finally region.
				if (nested.Kind == ControlFlowRegionKind.Try) {
					foreach (var nestedInner in nested.NestedRegions) {
						if (nestedInner.Kind == ControlFlowRegionKind.TryAndCatch) {
							foreach (var r in nestedInner.NestedRegions) {
								if (r.Kind == ControlFlowRegionKind.Catch)
									yield return new RegionProxy (r);
							}
						}
					}
				}
			}
		}

		public BlockProxy FirstBlock (RegionProxy region) =>
			new BlockProxy (ControlFlowGraph.Blocks[region.Region.FirstBlockOrdinal]);

		public BlockProxy LastBlock (RegionProxy region) =>
			new BlockProxy (ControlFlowGraph.Blocks[region.Region.LastBlockOrdinal]);
	}
}