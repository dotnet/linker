// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using MultiValue = ILLink.Shared.HashSetWrapper<ILLink.Shared.SingleValue>;
using StateValue = ILLink.RoslynAnalyzer.LocalState<ILLink.Shared.HashSetWrapper<ILLink.Shared.SingleValue>>;

namespace ILLink.RoslynAnalyzer
{
	public class DynamicallyAccessedMembersVisitor : LocalDataFlowVisitor<MultiValue, HashSetLattice<SingleValue>>
	{
		public readonly ReflectionAccessStore ReflectionAccesses;


		public DynamicallyAccessedMembersVisitor (
			LocalStateLattice<MultiValue, HashSetLattice<SingleValue>> lattice,
			OperationBlockAnalysisContext context
		) : base (lattice, context)
		{
			ReflectionAccesses = new ReflectionAccessStore ();
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - invocations (for annotated method returns)
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue VisitInvocation (IInvocationOperation operation, StateValue state)
		{
			// Base logic takes care of visiting arguments, etc.
			base.VisitInvocation (operation, state);

			return new MultiValue (new DynamicallyAccessedMembersSymbol (operation.TargetMethod, isMethodReturn: true));
		}

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return new MultiValue (new DynamicallyAccessedMembersSymbol (operation.OperatorMethod, isMethodReturn: true));

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return new MultiValue (new DynamicallyAccessedMembersSymbol (paramRef.Parameter));
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			var value = new MultiValue (new DynamicallyAccessedMembersSymbol ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: false));
			return value;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			return new MultiValue (new DynamicallyAccessedMembersSymbol (fieldRef.Field));
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			// TODO: track known types too!

			if (typeOfOperation.TypeOperand is ITypeParameterSymbol typeParameter)
				return new MultiValue (new DynamicallyAccessedMembersSymbol (typeParameter));

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in reflection access:
		// - assignments
		// - arguments passed to method parameters
		//   this also needs to create the annotated value for parmeters, because they are not represented
		//   as 'IParameterReferenceOperation' when passing arguments
		// - instance passed as explicit or implicit receiver to a method invocation
		//   this also needs to create the annotation for the implicit receiver parameter.
		// - value returned from a method

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation)
		{
			if (target.Equals (TopValue))
				return;

			ReflectionAccesses.Add (new ReflectionAccessPattern (source, target, operation));
		}

		public override void HandleArgument (MultiValue argumentValue, IArgumentOperation operation)
		{
			// Parameter may be null for __arglist arguments. Skip these.
			if (operation.Parameter == null)
				return;

			var parameter = new MultiValue (new DynamicallyAccessedMembersSymbol (operation.Parameter));

			ReflectionAccesses.Add (new ReflectionAccessPattern (
				argumentValue,
				parameter,
				operation
			));
		}

		public override void HandleReceiverArgument (MultiValue receieverValue, IInvocationOperation operation)
		{
			if (operation.Instance == null)
				return;

			MultiValue implicitReceiverParameter = new MultiValue (new DynamicallyAccessedMembersSymbol (operation.TargetMethod, isMethodReturn: false));

			ReflectionAccesses.Add (new ReflectionAccessPattern (
				receieverValue,
				implicitReceiverParameter,
				operation
			));
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var returnParameter = new MultiValue (new DynamicallyAccessedMembersSymbol ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: true));

			ReflectionAccesses.Add (new ReflectionAccessPattern (
				returnValue,
				returnParameter,
				operation
			));
		}
	}
}