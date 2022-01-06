// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace ILLink.Shared.DataFlow
{
	public enum RegionKind
	{
		Try,
		Catch,
		Finally
	}

	public interface IRegion<TRegion> : IEquatable<TRegion>
	{
		RegionKind Kind { get; }
	}

	public interface IControlFlowGraph<TBlock, TRegion>
		where TBlock : IEquatable<TBlock>
		where TRegion : IRegion<TRegion>
	{
		IEnumerable<TBlock> Blocks { get; }

		TBlock Entry { get; }

		// This does not include predecessor edges for exceptional control flow into
		// catch regions or finally regions. It also doesn't include edges for non-exceptional
		// control flow from try -> finally or from catch -> finally.

		// It does, however, include edges for non-exceptional control flow out of a finally region.
		IEnumerable<TBlock> GetPredecessors (TBlock block);

		bool TryGetEnclosingTry (TBlock block, [NotNullWhen (true)] out TRegion? region);

		bool TryGetEnclosingCatch (TBlock block, [NotNullWhen (true)] out TRegion? region);

		bool TryGetEnclosingFinally (TBlock block, [NotNullWhen (true)] out TRegion? region);


		TRegion GetCorrespondingTry (TRegion cathOrFinallyRegion);

		IEnumerable<TRegion> GetCorrespondingCatch (TRegion finallyRegion);

		bool TryGetEnclosingTryOrCatch (TRegion region, out TRegion tryOrCatchRegion);

		TBlock FirstBlock (TRegion region);
	}
}