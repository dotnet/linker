using System;
using System.Collections.Generic;

namespace ILLink.Shared
{
	public interface IControlFlowGraph<TBlock>
		where TBlock : IEquatable<TBlock>
	{
		TBlock Entry { get; }

		IEnumerable<TBlock> Blocks { get; }

		IEnumerable<TBlock> GetPredecessors (TBlock block);
	}
}