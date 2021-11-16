// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalState<ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public class TrimAnalysisVisitor : LocalDataFlowVisitor<MultiValue, ValueSetLattice<SingleValue>>
	{
		public readonly TrimAnalysisPatternStore TrimAnalysisPatterns;


		public TrimAnalysisVisitor (
			LocalStateLattice<MultiValue, ValueSetLattice<SingleValue>> lattice,
			OperationBlockAnalysisContext context
		) : base (lattice, context)
		{
			TrimAnalysisPatterns = new TrimAnalysisPatternStore ();
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

			// TODO: don't track values for unsupported types. Can be done when adding warnings
			// for annotations on unsupported types.
			// https://github.com/dotnet/linker/issues/2273
			return new MultiValue (new SymbolValue (operation.TargetMethod, isMethodReturn: true));
		}

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return new MultiValue (new SymbolValue (operation.OperatorMethod, isMethodReturn: true));

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return new MultiValue (new SymbolValue (paramRef.Parameter));
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			var value = new MultiValue (new SymbolValue ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: false));
			return value;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			return new MultiValue (new SymbolValue (fieldRef.Field));
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			// TODO: track known types too!

			if (typeOfOperation.TypeOperand is ITypeParameterSymbol typeParameter)
				return new MultiValue (new SymbolValue (typeParameter));

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in reflection access:
		// - assignments
		// - arguments passed to method parameters
		//   this also needs to create the annotated value for parameters, because they are not represented
		//   as 'IParameterReferenceOperation' when passing arguments
		// - instance passed as explicit or implicit receiver to a method invocation
		//   this also needs to create the annotation for the implicit receiver parameter.
		// - value returned from a method

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation)
		{
			if (target.Equals (TopValue))
				return;

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (source, target, operation));
		}

		public override void HandleArgument (MultiValue argumentValue, IArgumentOperation operation)
		{
			// Parameter may be null for __arglist arguments. Skip these.
			if (operation.Parameter == null)
				return;

			var parameter = new MultiValue (new SymbolValue (operation.Parameter));

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				argumentValue,
				parameter,
				operation
			));
		}

		public override void HandleReceiverArgument (MultiValue receieverValue, IInvocationOperation operation)
		{
			if (operation.Instance == null)
				return;

			MultiValue implicitReceiverParameter = new MultiValue (new SymbolValue (operation.TargetMethod, isMethodReturn: false));

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				receieverValue,
				implicitReceiverParameter,
				operation
			));
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var returnParameter = new MultiValue (new SymbolValue ((IMethodSymbol) Context.OwningSymbol, isMethodReturn: true));

			TrimAnalysisPatterns.Add (new TrimAnalysisPattern (
				returnValue,
				returnParameter,
				operation
			));
		}
	}
}