// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Visitor which tracks the values of locals in a block. It provides extension points that get called
	// whenever a value that comes from a tracked local reference flows into one of the following:
	// - field
	// - parameter
	// - method return
	public abstract class LocalDataFlowVisitor<TValue, TValueLattice> : OperationVisitor<DataFlowState<LocalState<TValue>>, TValue>,
		ITransfer<BlockProxy, LocalState<TValue>, LocalStateLattice<TValue, TValueLattice>>
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		protected readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;

		protected readonly OperationBlockAnalysisContext Context;

		protected TValue TopValue => LocalStateLattice.Lattice.ValueLattice.Top;

		public LocalDataFlowVisitor (LocalStateLattice<TValue, TValueLattice> lattice, OperationBlockAnalysisContext context) =>
			(LocalStateLattice, Context) = (lattice, context);

		public void Transfer (BlockProxy block, DataFlowState<LocalState<TValue>> state)
		{
			foreach (IOperation operation in block.Block.Operations)
				Visit (operation, state);

			// Blocks may end with a BranchValue computation. Visit the BranchValue operation after all others.
			IOperation? branchValueOperation = block.Block.BranchValue;
			if (branchValueOperation == null)
				return;

			var branchValue = Visit (branchValueOperation, state);

			// BranchValue may represent a value used in a conditional branch to the ConditionalSuccessor - if so, we are done.
			if (block.Block.ConditionKind != ControlFlowConditionKind.None)
				return;

			// If not, the BranchValue represents a return or throw value associated with the FallThroughSuccessor of this block.
			// (ConditionalSuccessor == null iff ConditionKind == None).

			// The BranchValue for a thrown value is not involved in dataflow tracking.
			if (block.Block.FallThroughSuccessor?.Semantics == ControlFlowBranchSemantics.Throw)
				return;

			// Return statements with return values are represented in the control flow graph as
			// a branch value operation that computes the return value.

			// Use the branch value operation as the key for the warning store and the location of the warning.
			// We don't want the return operation because this might have multiple possible return values in general.
			HandleReturnValue (branchValue, branchValueOperation);
		}

		public abstract void HandleAssignment (TValue source, TValue target, IOperation operation);

		// This is called to handle instance method invocations, where "receiver" is the
		// analyzed value for the object on which the instance method is called, and similarly
		// for property references.
		public abstract void HandleReceiverArgument (TValue receiver, IMethodSymbol targetMethod, IOperation operation);

		public abstract void HandleArgument (TValue argument, IArgumentOperation operation);

		// Called for property setters which are essentially like arguments passed to a method.
		public abstract void HandlePropertySetterArgument (TValue value, IMethodSymbol setMethod, ISimpleAssignmentOperation operation);

		// This takes an IOperation rather than an IReturnOperation because the return value
		// may (must?) come from BranchValue of an operation whose FallThroughSuccessor is the exit block.
		public abstract void HandleReturnValue (TValue returnValue, IOperation operation);

		// Override with a non-nullable return value to prevent some warnings.
		// The interface constraint on TValue ensures that it's not a nullable type, so this is safe as long
		// as no overrides return default(TValue).
		public override TValue Visit (IOperation? operation, DataFlowState<LocalState<TValue>> argument) => operation != null ? operation.Accept (this, argument)! : TopValue;

		internal virtual TValue VisitNoneOperation (IOperation operation, DataFlowState<LocalState<TValue>> argument) => TopValue;

		// The default visitor preserves the local state. Any unimplemented operations will not
		// have their effects reflected in the tracked state.
		public override TValue DefaultVisit (IOperation operation, DataFlowState<LocalState<TValue>> state) => TopValue;

		public override TValue VisitLocalReference (ILocalReferenceOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			return state.Current.Get (new LocalKey (operation.Local));
		}

		public override TValue VisitSimpleAssignment (ISimpleAssignmentOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			var targetValue = Visit (operation.Target, state);
			var value = Visit (operation.Value, state);
			switch (operation.Target) {
			case ILocalReferenceOperation localRef:
				state.Current.Set (new LocalKey (localRef.Local), value);
				if (state.Exception != null)
					state.Exception.Value = LocalStateLattice.Meet (state.Exception.Value, state.Current);
				break;
			case IFieldReferenceOperation:
			case IParameterReferenceOperation:
				// Extension point for assignments to "interesting" targets.
				// Doesn't get called for assignments to locals, which are handled above.
				HandleAssignment (value, targetValue, operation);
				break;
			case IPropertyReferenceOperation propertyRef:
				// A property assignment is really a call to the property setter.
				var setMethod = propertyRef.Property.SetMethod;
				Debug.Assert (setMethod != null);
				HandlePropertySetterArgument (value, setMethod!, operation);
				break;
			// TODO: when setting a property in an attribute, target is an IPropertyReference.
			case IArrayElementReferenceOperation:
				// TODO
				break;
			case IDiscardOperation:
				// Assignments like "_ = SomeMethod();" don't need dataflow tracking.
				break;
			default:
				throw new NotImplementedException (operation.Target.GetType ().ToString ());
			}
			return value;
		}

		// Similar to VisitLocalReference
		public override TValue VisitFlowCaptureReference (IFlowCaptureReferenceOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			return state.Current.Get (new LocalKey (operation.Id));
		}

		// Similar to VisitSimpleAssignment when assigning to a local, but for values which are captured without a
		// corresponding local variable. The "flow capture" is like a local assignment, and the "flow capture reference"
		// is like a local reference.
		public override TValue VisitFlowCapture (IFlowCaptureOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			TValue value = Visit (operation.Value, state);
			state.Current.Set (new LocalKey (operation.Id), value);
			if (state.Exception != null)
				// TODO: optimize this to not meet the whole value, but just modify one value without copying.
				state.Exception.Value = LocalStateLattice.Meet (state.Exception.Value, state.Current);
			return value;
		}

		public override TValue VisitExpressionStatement (IExpressionStatementOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			Visit (operation.Operation, state);
			return TopValue;
		}

		public override TValue VisitInvocation (IInvocationOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			if (operation.Instance != null) {
				var instanceValue = Visit (operation.Instance, state);
				HandleReceiverArgument (instanceValue, operation.TargetMethod, operation);
			}

			foreach (var argument in operation.Arguments)
				VisitArgument (argument, state);

			return TopValue;
		}

		public static IMethodSymbol GetPropertyMethod (IPropertyReferenceOperation operation)
		{
			// The IPropertyReferenceOperation doesn't tell us whether this reference is to the getter or setter.
			// For this we need to look at the containing operation.
			var parent = operation.Parent;
			if (parent?.Kind == OperationKind.SimpleAssignment) {
				var assignment = (ISimpleAssignmentOperation) parent;
				if (assignment.Target == operation) {
					var setMethod = operation.Property.SetMethod;
					Debug.Assert (setMethod != null);
					return setMethod!;
				}
				Debug.Assert (assignment.Value == operation);
			}

			var getMethod = operation.Property.GetMethod;
			Debug.Assert (getMethod != null);
			return getMethod!;
		}

		public override TValue VisitPropertyReference (IPropertyReferenceOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			if (operation.Instance != null) {
				var instanceValue = Visit (operation.Instance, state);
				HandleReceiverArgument (instanceValue, GetPropertyMethod (operation), operation);
			}

			return TopValue;
		}

		public override TValue VisitArgument (IArgumentOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			var value = Visit (operation.Value, state);
			HandleArgument (value, operation);
			return value;
		}

		public override TValue VisitReturn (IReturnOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			if (operation.ReturnedValue != null) {
				var value = Visit (operation.ReturnedValue, state);
				HandleReturnValue (value, operation);
				return value;
			}

			return TopValue;
		}

		public override TValue VisitConversion (IConversionOperation operation, DataFlowState<LocalState<TValue>> state)
		{
			var operandValue = Visit (operation.Operand, state);
			return operation.OperatorMethod == null ? operandValue : TopValue;
		}
	}
}