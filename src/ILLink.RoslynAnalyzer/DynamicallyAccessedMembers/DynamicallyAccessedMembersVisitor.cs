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
		// which reference annotated source locations:
		// - invocations (for annotated method returns)
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue VisitInvocation (IInvocationOperation operation, StateValue state)
		{
			// Base logic visits arguments, etc.
			base.VisitInvocation (operation, state);

			// TODO: don't track unannotated locations

			//			var damt = operation.TargetMethod.GetDynamicallyAccessedMemberTypesOnReturnType ();
			return new MultiValue (new AnnotatedSymbol (operation.TargetMethod, isMethodReturn: true));
		}

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return new MultiValue (new AnnotatedSymbol (operation.OperatorMethod, isMethodReturn: true));

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			// TODO: don't track unannotated locations

			// var damt = paramRef.Parameter.GetDynamicallyAccessedMemberTypes ();
			var value = new MultiValue (new AnnotatedSymbol (paramRef.Parameter));

			return value;
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// 'this' or 'base', we get annotation from the containing method.
			// TODO: ensure this works even for lambdas, etc.
			// Not sure if the context OwningSymbol is the right thing always.
			// var damt = Context.OwningSymbol.GetDynamicallyAccessedMemberTypes ();
			var value = new MultiValue (new AnnotatedSymbol ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: false));
			return value;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			// TODO: don't track unannotated fields

			// var damt = fieldRef.Field.GetDynamicallyAccessedMemberTypes ();
			return new MultiValue (new AnnotatedSymbol (fieldRef.Field));
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			// TODO: track known types too!

			// We only need to find the symbol for generic types here
			if (typeOfOperation.TypeOperand is ITypeParameterSymbol typeParameter)
				return new MultiValue (new AnnotatedSymbol (typeParameter));

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in dataflow:
		// - assignments
		// - arguments passed to method parameters
		//   this also needs to create the annotated value for parmeters, because they are not represented
		//   as 'IParameterReferenceOperation'.
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
			// Parameter may be null for __arglist arguments.
			// skip these.
			if (operation.Parameter == null)
				return;

			// TODO: skip unannotated parameters
			var parameter = new MultiValue (new AnnotatedSymbol (operation.Parameter));

			var accessPattern = new ReflectionAccessPattern (
				argumentValue,
				parameter,
				operation
			);
			ReflectionAccesses.Add (accessPattern);
		}

		public override void HandleReceiverArgument (MultiValue receieverValue, IInvocationOperation operation)
		{
			if (operation.Instance == null)
				return;

			MultiValue implicitReceiverParameter = new MultiValue (new AnnotatedSymbol (operation.TargetMethod, isMethodReturn: false));

			// TODO: skip unannotated receiever parameter?

			ReflectionAccesses.Add (new ReflectionAccessPattern (
				receieverValue,
				implicitReceiverParameter,
				operation
			));
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			// TODO: skip unannotated return?
			var returnParameter = new MultiValue (new AnnotatedSymbol ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: true));

			ReflectionAccesses.Add (new ReflectionAccessPattern (
				returnValue,
				returnParameter,
				operation
			));
		}
	}
}