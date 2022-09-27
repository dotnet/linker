// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;


namespace Mono.Linker.Dataflow
{
	sealed class ReflectionDataFlowAnalysis
		: ForwardDataFlowAnalysis<
			BasicBlockState<MultiValue>,
			BasicBlockDataFlowState<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>>,
			BlockStateLattice<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>>,
			BasicBlock,
			Region,
			ControlFlowGraph,
			ReflectionScanner
		>
	{
		private readonly BlockStateLattice<MultiValue, ValueSetLatticeWithUnknownValue<SingleValue>> _lattice;
		private readonly InterproceduralStateLattice _interproceduralStateLattice;
		private readonly LinkContext _context;
		private readonly TrimAnalysisPatternStore _trimAnalysisPatternStore;


		public ReflectionDataFlowAnalysis (LinkContext context)
		{
			_lattice = new (new ValueSetLatticeWithUnknownValue<SingleValue> ());
			_interproceduralStateLattice = default;
			_context = context;
			_trimAnalysisPatternStore = new TrimAnalysisPatternStore (_lattice.LocalsLattice.ValueLattice, context);
		}

		public void InterproceduralScan (MethodDefinition startingMethod, MarkStep parent)
		{
			var interproceduralState = _interproceduralStateLattice.Top;

			var oldInterproceduralState = interproceduralState.Clone ();
			interproceduralState.TrackMethod (startingMethod);

			while (!interproceduralState.Equals (oldInterproceduralState)) {
				oldInterproceduralState = interproceduralState.Clone ();

				// Flow state through all methods encountered so far, as long as there
				// are changes discovered in the hoisted local state on entry to any method.
				foreach (var methodBodyValue in oldInterproceduralState.MethodBodies)
					AnalyzeMethod (methodBodyValue.MethodBody, parent, ref interproceduralState);
			}

			var reflectionMarker = new ReflectionMarker (_context, parent, enabled: true);
			_trimAnalysisPatternStore.MarkAndProduceDiagnostics (reflectionMarker, parent);
		}

		private void AnalyzeMethod (MethodBody methodBody, MarkStep parent, ref InterproceduralState interproceduralState)
		{
			var reflectionHandler = new ReflectionHandler (_context, parent, new MessageOrigin (methodBody.Method), _trimAnalysisPatternStore);
			if (!TryControlFlowGraphScan (methodBody, reflectionHandler, ref interproceduralState)) {
				var scanner = new ReflectionMethodBodyScanner (_context, reflectionHandler);
				scanner.Scan (methodBody, ref interproceduralState);
			}
		}

		private bool TryControlFlowGraphScan (MethodBody methodBody, ReflectionHandler handler, ref InterproceduralState interproceduralState)
		{
			if (ControlFlowGraph.TryCreate (methodBody, out var cfg)) {
				var bodyScanner = new ReflectionScanner (_context, handler, interproceduralState);
				Fixpoint (cfg, _lattice, bodyScanner);

				bodyScanner.HandleReturnValue (methodBody.Method);

				// The interprocedural state struct is stored as a field of the visitor and modified
				// in-place there, but we also need those modifications to be reflected here.
				interproceduralState = bodyScanner.InterproceduralState;
				return true;
			}

			return false;
		}

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition? methodDefinition = context.TryResolve (calledMethod);
			if (methodDefinition == null)
				return false;

			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
				context.Annotations.DoesMethodRequireUnreferencedCode (methodDefinition, out _) ||
				methodDefinition.IsPInvokeImpl && ComHandler.ComDangerousMethod (methodDefinition, context);
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (LinkContext context, MethodDefinition methodDefinition)
		{
			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
		}

		public static bool RequiresReflectionMethodBodyScannerForAccess (LinkContext context, FieldReference field)
		{
			FieldDefinition? fieldDefinition = context.TryResolve (field);
			if (fieldDefinition == null)
				return false;

			return context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (fieldDefinition);
		}
	}
}
