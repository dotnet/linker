// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	abstract class LocalDataFlowAnalysis<TTransfer>
		: ForwardDataFlowAnalysis<
			BasicBlockState<MultiValue>,
			BasicBlockDataFlowState<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>>,
			BlockStateLattice<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>>,
			BasicBlock,
			Region,
			ControlFlowGraph,
			TTransfer
		>
		where TTransfer : Scanner
	{
		protected readonly BlockStateLattice<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>> Lattice;

		protected readonly LinkContext Context;

		protected LocalDataFlowAnalysis (LinkContext context)
		{
			Lattice = new (new ValueSetLatticeWithUnknownValue<SingleValue> ());
			Context = context;
		}

		public bool TryAnalyzeMethod (MethodDefinition method, MarkStep parent, MessageOrigin origin)
		{
			if (ControlFlowGraph.TryCreate (method.Body, out var cfg) && !Context.CompilerGeneratedState.TryGetCompilerGeneratedCalleesForUserMethod (method, out List<IMemberDefinition>? _)) {
				var bodyScanner = GetBodyScanner (Context, parent, origin);
				Fixpoint (cfg, Lattice, bodyScanner);
				return true;
			}

			return false;
		}

		protected abstract TTransfer GetBodyScanner (
			LinkContext context, MarkStep parent, MessageOrigin origin);
	}
}
