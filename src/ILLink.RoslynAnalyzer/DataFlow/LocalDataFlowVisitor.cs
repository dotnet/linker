// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
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
	public abstract class LocalDataFlowVisitor<TValue, TValueLattice> : OperationWalker<LocalDataFlowState<TValue, TValueLattice>, TValue>,
		ITransfer<BlockProxy, LocalState<TValue>, LocalDataFlowState<TValue, TValueLattice>, LocalStateLattice<TValue, TValueLattice>>
		// This struct constraint prevents warnings due to possible null returns from the visitor methods.
		// Note that this assumes that default(TValue) is equal to the TopValue.
		where TValue : struct, IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		protected readonly LocalStateLattice<TValue, TValueLattice> LocalStateLattice;

		protected readonly OperationBlockAnalysisContext Context;

		protected TValue TopValue => LocalStateLattice.Lattice.ValueLattice.Top;

		private readonly ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures;

		bool IsLValueFlowCapture (CaptureId captureId)
			=> lValueFlowCaptures.ContainsKey (captureId);

		bool IsRValueFlowCapture (CaptureId captureId)
			=> !lValueFlowCaptures.TryGetValue (captureId, out var captureKind) || captureKind != FlowCaptureKind.LValueCapture;

		public LocalDataFlowVisitor (LocalStateLattice<TValue, TValueLattice> lattice, OperationBlockAnalysisContext context, ImmutableDictionary<CaptureId, FlowCaptureKind> lValueFlowCaptures) =>
			(LocalStateLattice, Context, this.lValueFlowCaptures) = (lattice, context, lValueFlowCaptures);

		public void Transfer (BlockProxy block, LocalDataFlowState<TValue, TValueLattice> state)
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

		public abstract TValue HandleArrayElementRead (TValue arrayValue, TValue indexValue, IOperation operation);

		public abstract void HandleArrayElementWrite (TValue arrayValue, TValue indexValue, TValue valueToWrite, IOperation operation);

		// This takes an IOperation rather than an IReturnOperation because the return value
		// may (must?) come from BranchValue of an operation whose FallThroughSuccessor is the exit block.
		public abstract void HandleReturnValue (TValue returnValue, IOperation operation);

		// This is called for any method call, which includes:
		// - Normal invocation operation
		// - Accessing property value - which is treated as a call to the getter
		// - Setting a property value - which is treated as a call to the setter
		// All inputs are already visited and turned into values.
		// The return value should be a value representing the return value from the called method.
		public abstract TValue HandleMethodCall (IMethodSymbol calledMethod, TValue instance, ImmutableArray<TValue> arguments, IOperation operation);

		public override TValue VisitLocalReference (ILocalReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			return state.Get (new LocalKey (operation.Local));
		}

		public void ProcessPropertyAssignment (ISimpleAssignmentOperation operation, IPropertyReferenceOperation propertyReference, TValue value, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var setMethod = propertyReference.Property.SetMethod;
			if (setMethod == null) {
				// This can happen in a constructor - there it is possible to assign to a property
				// without a setter. This turns into an assignment to the compiler-generated backing field.
				// To match the linker, this should warn about the compiler-generated backing field.
				// For now, just don't warn. https://github.com/dotnet/linker/issues/2731
				return;
			}
			TValue instanceValue = Visit (propertyReference.Instance, state);
			// The return value of a property set expression is the value,
			// even though a property setter has no return value.
			HandleMethodCall (
				setMethod,
				instanceValue,
				ImmutableArray.Create (value),
				operation);
		}

		public void HandleAssignment (ISimpleAssignmentOperation operation, IOperation targetOperation, LocalDataFlowState<TValue, TValueLattice> state, TValue targetValue, TValue value)
		{
			switch (targetOperation) {
			case IPropertyReferenceOperation propertyRef:
				ProcessPropertyAssignment (operation, propertyRef, value, state);
				break;
			// TODO: when setting a property in an attribute, target is an IPropertyReference.
			case ILocalReferenceOperation localRef:
				state.Set (new LocalKey (localRef.Local), value);
				break;
			case IFieldReferenceOperation:
			case IParameterReferenceOperation:
				// The field/parameter reference hasn't been visited yet if it's a FlowCaptureReference.
				// TODO: but it doesn't need to be visited again for normal assignments
				// TODO: is the order of operations correct here?
				targetValue = Visit (targetOperation, state);
				HandleAssignment (value, targetValue, operation);
				break;
			case IArrayElementReferenceOperation arrayElementRef:
				if (arrayElementRef.Indices.Length != 1)
					break;

				// TODO: is the order of operations correct here?
				HandleArrayElementWrite (Visit (arrayElementRef.ArrayReference, state), Visit (arrayElementRef.Indices[0], state), value, operation);
				break;
			case IDiscardOperation:
				// Assignments like "_ = SomeMethod();" don't need dataflow tracking.
				// Seems like this can't happen with a flow capture operation.
				Debug.Assert (operation.Target is not IFlowCaptureReferenceOperation);
				break;
			case IInvalidOperation:
				// This can happen for a field assignment in an attribute instance.
				// TODO: validate against the field attributes.
				break;
			default:
				throw new NotImplementedException (targetOperation.GetType ().ToString ());
			}
		}

		public override TValue VisitSimpleAssignment (ISimpleAssignmentOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var targetValue = Visit (operation.Target, state);
			var value = Visit (operation.Value, state);

			var targetOperation = operation.Target;
			if (targetOperation is IFlowCaptureReferenceOperation flowCaptureReference) {
				var capturedReference = state.Current.CapturedReferences.Get (flowCaptureReference.Id).Reference;
				Debug.Assert (IsLValueFlowCapture (flowCaptureReference.Id));
				targetOperation = capturedReference;
			}

			HandleAssignment (operation, targetOperation, state, targetValue, value);
			return value;
		}

		// Similar to VisitLocalReference
		public override TValue VisitFlowCaptureReference (IFlowCaptureReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (!operation.GetValueUsageInfo (Context.OwningSymbol).HasFlag (ValueUsageInfo.Read)) {
				Debug.Assert (IsLValueFlowCapture (operation.Id));
				return TopValue;
			}

			Debug.Assert (IsRValueFlowCapture (operation.Id));
			return state.Get (new LocalKey (operation.Id));
		}

		// Similar to VisitSimpleAssignment when assigning to a local, but for values which are captured without a
		// corresponding local variable. The "flow capture" is like a local assignment, and the "flow capture reference"
		// is like a local reference.
		public override TValue VisitFlowCapture (IFlowCaptureOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			TValue value = Visit (operation.Value, state);
			if (IsLValueFlowCapture (operation.Id)) {
				var currentState = state.Current;
				currentState.CapturedReferences.Set (operation.Id, new CapturedReferenceValue (operation.Value));
				state.Current = currentState;
			}
			if (IsRValueFlowCapture (operation.Id))
				state.Set (new LocalKey (operation.Id), value);

			return TopValue;
		}

		public override TValue VisitExpressionStatement (IExpressionStatementOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			Visit (operation.Operation, state);
			return TopValue;
		}

		public override TValue VisitInvocation (IInvocationOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
			=> ProcessMethodCall (operation, operation.TargetMethod, operation.Instance, operation.Arguments, state);

		public override TValue VisitPropertyReference (IPropertyReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (!operation.GetValueUsageInfo (Context.OwningSymbol).HasFlag (ValueUsageInfo.Read))
				return TopValue;

			// Accessing property for reading is really a call to the getter
			// The setter case is handled in assignment operation since here we don't have access to the value to pass to the setter
			TValue instanceValue = Visit (operation.Instance, state);
			return HandleMethodCall (
				operation.Property.GetMethod!,
				instanceValue,
				ImmutableArray<TValue>.Empty,
				operation);
		}

		public override TValue VisitArrayElementReference (IArrayElementReferenceOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (!operation.GetValueUsageInfo (Context.OwningSymbol).HasFlag (ValueUsageInfo.Read))
				return TopValue;

			// Accessing an array element for reading is a call to the indexer
			// or a plain array access. Just handle plain array access for now.

			// Only handle simple index access
			if (operation.Indices.Length != 1)
				return TopValue;

			return HandleArrayElementRead (Visit (operation.ArrayReference, state), Visit (operation.Indices[0], state), operation);
		}

		public override TValue VisitArgument (IArgumentOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			return Visit (operation.Value, state);
		}

		public override TValue VisitReturn (IReturnOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.ReturnedValue != null) {
				var value = Visit (operation.ReturnedValue, state);
				HandleReturnValue (value, operation);
				return value;
			}

			return TopValue;
		}

		public override TValue VisitConversion (IConversionOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			var operandValue = Visit (operation.Operand, state);
			return operation.OperatorMethod == null ? operandValue : TopValue;
		}

		public override TValue VisitObjectCreation (IObjectCreationOperation operation, LocalDataFlowState<TValue, TValueLattice> state)
		{
			if (operation.Constructor == null)
				return TopValue;

			return ProcessMethodCall (operation, operation.Constructor, null, operation.Arguments, state);
		}

		TValue ProcessMethodCall (
			IOperation operation,
			IMethodSymbol method,
			IOperation? instance,
			ImmutableArray<IArgumentOperation> arguments,
			LocalDataFlowState<TValue, TValueLattice> state)
		{
			TValue instanceValue = Visit (instance, state);

			var argumentsBuilder = ImmutableArray.CreateBuilder<TValue> ();
			foreach (var argument in arguments) {
				// For __arglist argument there might not be any parameter
				// __arglist is only legal as the last argument to a method and there's also no supported
				// way for it to carry annotations or participate in data flow in any way, so it's OK to ignore it.
				// Since it's always last, it's also OK to simply pass a shorter arguments array to the call.
				if (argument?.Parameter == null)
					break;

				argumentsBuilder.Add (VisitArgument (argument, state));
			}

			return HandleMethodCall (
				method,
				instanceValue,
				argumentsBuilder.ToImmutableArray (),
				operation);
		}
	}
}
