using System;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	// Visitor which tracks the values of locals in a block.
	// It provides extension points that get called whenever a value that comes from a local
	// reference flows into one of the following:
	// - field
	// - parameter
	// - method return
	public abstract class LocalDataFlowVisitor<TValue, TValueLattice> : OperationVisitor<LocalState<TValue>, TValue>,
		ITransfer<BlockWrapper, LocalState<TValue>, LocalStateLattice<TValue, TValueLattice>>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		protected readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;

		protected TValue TopValue => LocalStateLattice.Lattice.ValueLattice.Top;

		public LocalDataFlowVisitor (LocalStateLattice<TValue, TValueLattice> lattice) => LocalStateLattice = lattice;

		public void Transfer (BlockWrapper block, LocalState<TValue> state)
		{
			foreach (IOperation operation in block.Block.Operations)
				Visit (operation, state);

			// Note: ConditionKind != None iff ConditionalSuccessor != null.
			// But BranchValue may be non-null either way - for None ConditionKind, this just means
			// that the BranchValue represents a return or throw value associated with the FallThroughSuccessor.
			// TODO: maybe this needs to be used to detect return values.
			// if (block.Block.ConditionKind != ControlFlowConditionKind.None)
			IOperation? branchValueOperation = block.Block.BranchValue;
			if (branchValueOperation != null) {
				var branchValue = Visit (branchValueOperation, state);
				if (block.Block.ConditionKind == ControlFlowConditionKind.None) {
					// this means it's a return value or throw value associated with the fall-through successor.
					// TODO: how to deal with throw values?

					HandleReturnValue (branchValue, branchValueOperation);
				}
			}
		}

#pragma warning disable CS8765
		// TODO: do we really want to override with a non-nullable return value?
		// What about operations which don't have a return value? It may be OK to model both
		// (untracked values, and the result of operations which don't return anything) as Top values.
		public override TValue Visit (IOperation operation, LocalState<TValue> argument) => operation.Accept (this, argument)!;
#pragma warning restore CS8765

		public override TValue DefaultVisit (IOperation operation, LocalState<TValue> state)
		{
			// The default visitor preserves the local state. Any unimplemented operations will not
			// have their effects tracked in the state.
			return LocalStateLattice.Lattice.ValueLattice.Top;
		}

		public override TValue? VisitLocalReference (ILocalReferenceOperation operation, LocalState<TValue> state)
		{
			return state.Get (new LocalKey (operation.Local));
		}

		public override TValue? VisitSimpleAssignment (ISimpleAssignmentOperation operation, LocalState<TValue> state)
		{
			var targetValue = Visit (operation.Target, state);
			var value = Visit (operation.Value, state);
			switch (operation.Target) {
			case ILocalReferenceOperation localRef:
				state.Set (new LocalKey (localRef.Local), value);
				break;
			case IFieldReferenceOperation:
			case IParameterReferenceOperation:
				// Extension point for assignments to "interesting" targets.
				// Doesn't get called for assignments to locals, which are handled above.
				// Or would it be useful for the extension point to include local assignments?
				HandleAssignment (value, targetValue, operation);
				break;
			case IPropertyReferenceOperation:
			// TODO. attribute property setters
			case IArrayElementReferenceOperation:
				// TODO: array[0] in MethodReturnParameterDataFlow.
				// we don't track array elements yet
				break;
			default:
				throw new NotImplementedException (operation.Target.GetType ().ToString ());
			}
			return value;
		}

		public abstract void HandleAssignment (TValue source, TValue target, IOperation operation);

		public abstract void HandleReceiverArgument (TValue receiver, IInvocationOperation operation);

		public abstract void HandleArgument (TValue argument, IArgumentOperation operation);

		// This doesn't necessarily take an IReturnOperation (does it ever?)
		// The return value may come from BranchValue of an opertaion whose FallThroughSuccessor
		// is the exit block.
		public abstract void HandleReturnValue (TValue returnValue, IOperation operation);

		// Similar to VisitLocalReference
		public override TValue? VisitFlowCaptureReference (IFlowCaptureReferenceOperation operation, LocalState<TValue> state)
		{
			return state.Get (new LocalKey (operation.Id));
		}

		// Similar to VisitSimpleAssignment when assigning to a local, but for values which are captured without a
		// corresponding local variable.
		// The "flow capture" is like a local assignment, and the "flow capture reference" is like a local reference.
		public override TValue? VisitFlowCapture (IFlowCaptureOperation operation, LocalState<TValue> state)
		{
			TValue value = Visit (operation.Value, state);
			state.Set (new LocalKey (operation.Id), value);
			return value;
		}

		public override TValue? VisitExpressionStatement (IExpressionStatementOperation operation, LocalState<TValue> state)
		{
			Visit (operation.Operation, state);
			return TopValue;
		}

		public override TValue? VisitInvocation (IInvocationOperation operation, LocalState<TValue> state)
		{
			if (operation.Instance != null) {
				var instanceValue = Visit (operation.Instance, state);
				HandleReceiverArgument (instanceValue, operation);
			}

			foreach (var argument in operation.Arguments)
				VisitArgument (argument, state);

			return TopValue;
		}

		public override TValue? VisitArgument (IArgumentOperation operation, LocalState<TValue> state)
		{
			var value = Visit (operation.Value, state);
			HandleArgument (value, operation);
			return value;
		}

		public override TValue? VisitReturn (IReturnOperation operation, LocalState<TValue> state)
		{
			if (operation.ReturnedValue != null) {
				var value = Visit (operation.ReturnedValue, state);
				HandleReturnValue (value, operation);
				return value;
			}

			return TopValue;
		}
	}
}