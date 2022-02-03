// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using ILLink.RoslynAnalyzer.DataFlow;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;
using StateValue = ILLink.RoslynAnalyzer.DataFlow.LocalDataFlowState<
	ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>,
	ILLink.Shared.DataFlow.ValueSetLattice<ILLink.Shared.DataFlow.SingleValue>
	>;

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
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (lattice.Lattice.ValueLattice);
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return new MethodReturnValue (operation.OperatorMethod);

			// TODO - is it possible to have annotation on the operator method parameters?
			// if so, will these be checked here?

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return new MethodParameterValue (paramRef.Parameter);
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			var value = new MethodThisParameterValue ((IMethodSymbol) Context.OwningSymbol);
			return value;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			return new FieldValue (fieldRef.Field);
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			// TODO: track known types too!

			if (typeOfOperation.TypeOperand is ITypeParameterSymbol typeParameter)
				return new GenericParameterValue (typeParameter);
			else if (typeOfOperation.TypeOperand is INamedTypeSymbol namedType)
				return new SystemTypeValue (namedType);

			return TopValue;
		}

		// Override handlers for situations where annotated locations may be involved in reflection access:
		// - assignments
		// - method calls
		// - value returned from a method

		public override void HandleAssignment (MultiValue source, MultiValue target, IOperation operation)
		{
			if (target.Equals (TopValue))
				return;

			// TODO: consider not tracking patterns unless the target is something
			// annotated with DAMT.
			TrimAnalysisPatterns.Add (
				new TrimAnalysisAssignmentPattern (source, target, operation),
				isReturnValue: false
			);
		}

		public override MultiValue HandleMethodCall (IMethodSymbol calledMethod, MultiValue instance, ImmutableArray<MultiValue> arguments, IOperation operation)
		{
			// For .ctors:
			// - The instance value is empty (TopValue) and that's a bit wrong.
			//   Technically this is an instance call and the instance is some valid value, we just don't know which
			//   but for example it does have a static type. For now this is OK since we don't need the information
			//   for anything yet.
			// - The return here is also technically problematic, the return value is an instance of a known type,
			//   but currently we return empty (since the .ctor is declared as returning void).
			//   Especially with DAM on type, this can lead to incorrectly analyzed code (as in unknown type which leads
			//   to noise). Linker has the same problem currently: https://github.com/dotnet/linker/issues/1952

			var handleCallAction = new HandleCallAction (Context.OwningSymbol, operation);
			if (!handleCallAction.Invoke (new MethodProxy (calledMethod), instance, arguments, out MultiValue methodReturnValue)) {
				methodReturnValue = calledMethod.ReturnsVoid ? TopValue : new MethodReturnValue (calledMethod);
			}

			TrimAnalysisPatterns.Add (new TrimAnalysisMethodCallPattern (
				calledMethod,
				instance,
				arguments,
				operation,
				Context.OwningSymbol));

			return methodReturnValue;
		}

		public override void HandleReturnValue (MultiValue returnValue, IOperation operation)
		{
			var returnParameter = new MethodReturnValue ((IMethodSymbol) Context.OwningSymbol);

			TrimAnalysisPatterns.Add (
				new TrimAnalysisAssignmentPattern (returnValue, returnParameter, operation),
				isReturnValue: true
			);
		}
	}
}