// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
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

		readonly ValueSetLattice<SingleValue> _multiValueLattice;

		public TrimAnalysisVisitor (
			LocalStateLattice<MultiValue, ValueSetLattice<SingleValue>> lattice,
			OperationBlockAnalysisContext context
		) : base (lattice, context)
		{
			_multiValueLattice = lattice.Lattice.ValueLattice;
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (_multiValueLattice);
		}

		// Override visitor methods to create tracked values when visiting operations
		// which reference possibly annotated source locations:
		// - parameters
		// - 'this' parameter (for annotated methods)
		// - field reference

		public override MultiValue Visit (IOperation? operation, StateValue argument)
		{
			var returnValue = base.Visit (operation, argument);

			// If the return value is empty (TopValue basically) and the Operation tree
			// reports it as having a constant value, use that as it will automatically cover
			// cases we don't need/want to handle.
			if (operation != null && returnValue.IsEmpty () && operation.ConstantValue.HasValue) {
				object? constantValue = operation.ConstantValue.Value;
				if (constantValue == null)
					return NullValue.Instance;
				else if (operation.Type?.SpecialType == SpecialType.System_String && constantValue is string stringConstantValue)
					return new KnownStringValue (stringConstantValue);
				else if (operation.Type?.TypeKind == TypeKind.Enum && constantValue is int intConstantValue)
					return new ConstIntValue (intConstantValue);
			}

			return returnValue;
		}

		public override MultiValue VisitConversion (IConversionOperation operation, StateValue state)
		{
			var value = base.VisitConversion (operation, state);

			if (operation.OperatorMethod != null)
				return operation.OperatorMethod.ReturnType.IsTypeInterestingForDataflow () ? new MethodReturnValue (operation.OperatorMethod) : value;

			// TODO - is it possible to have annotation on the operator method parameters?
			// if so, will these be checked here?

			return value;
		}

		public override MultiValue VisitParameterReference (IParameterReferenceOperation paramRef, StateValue state)
		{
			return paramRef.Parameter.Type.IsTypeInterestingForDataflow () ? new MethodParameterValue (paramRef.Parameter) : TopValue;
		}

		public override MultiValue VisitInstanceReference (IInstanceReferenceOperation instanceRef, StateValue state)
		{
			if (instanceRef.ReferenceKind != InstanceReferenceKind.ContainingTypeInstance)
				return TopValue;

			// The instance reference operation represents a 'this' or 'base' reference to the containing type,
			// so we get the annotation from the containing method.
			// TODO: Check whether the Context.OwningSymbol is the containing type in case we are in a lambda.
			if (instanceRef.Type != null && instanceRef.Type.IsTypeInterestingForDataflow ())
				return new MethodThisParameterValue ((IMethodSymbol) Context.OwningSymbol);

			return TopValue;
		}

		public override MultiValue VisitFieldReference (IFieldReferenceOperation fieldRef, StateValue state)
		{
			if (fieldRef.Field.Type.IsTypeInterestingForDataflow ()) {
				var field = fieldRef.Field;
				if (field.Name is "Empty" && field.ContainingType.HasName ("System.String"))
					return new KnownStringValue (string.Empty);

				return new FieldValue (fieldRef.Field);
			}

			return TopValue;
		}

		public override MultiValue VisitTypeOf (ITypeOfOperation typeOfOperation, StateValue state)
		{
			var t = typeOfOperation.TypeOperand;
			return (t.Kind, t.ContainingNamespace?.Name, t.MetadataName, (t as INamedTypeSymbol)?.TypeArguments.FirstOrDefault ()?.TypeKind) switch {
				(SymbolKind.TypeParameter, _, _, _) =>
					new GenericParameterValue ((ITypeParameterSymbol) t),
				(SymbolKind.NamedType, "System", "Nullable`1", TypeKind.TypeParameter) =>
					new NullableValueWithDynamicallyAccessedMembers (new TypeProxy (t), new GenericParameterValue ((ITypeParameterSymbol) (t as INamedTypeSymbol)!.TypeArguments[0])),
				(SymbolKind.NamedType, "System", "Nullable`1", _) =>
					new NullableSystemTypeValue (new TypeProxy (t), new TypeProxy ((t as INamedTypeSymbol)!.TypeArguments[0])),
				(SymbolKind.NamedType, _, _, _) => 
					new SystemTypeValue (new TypeProxy (t)),
				(_, _, _, _) => TopValue
			};
		}

		public override MultiValue VisitBinaryOperator (IBinaryOperation operation, StateValue argument)
		{
			if (!operation.ConstantValue.HasValue && // Optimization - if there is already a constant value available, rely on the Visit(IOperation) instead
				operation.OperatorKind == BinaryOperatorKind.Or &&
				operation.OperatorMethod is null &&
				(operation.Type?.TypeKind == TypeKind.Enum || operation.Type?.SpecialType == SpecialType.System_Int32)) {
				MultiValue leftValue = Visit (operation.LeftOperand, argument);
				MultiValue rightValue = Visit (operation.RightOperand, argument);

				MultiValue result = TopValue;
				foreach (var left in leftValue) {
					if (left is UnknownValue)
						result = _multiValueLattice.Meet (result, left);
					else if (left is ConstIntValue leftConstInt) {
						foreach (var right in rightValue) {
							if (right is UnknownValue)
								result = _multiValueLattice.Meet (result, right);
							else if (right is ConstIntValue rightConstInt) {
								result = _multiValueLattice.Meet (result, new ConstIntValue (leftConstInt.Value | rightConstInt.Value));
							}
						}
					}
				}

				return result;
			}

			return base.VisitBinaryOperator (operation, argument);
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

		public override MultiValue HandleArrayElementAccess (IOperation arrayReferene)
		{
			return UnknownValue.Instance;
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

			var diagnosticContext = DiagnosticContext.CreateDisabled ();
			var handleCallAction = new HandleCallAction (diagnosticContext, Context.OwningSymbol, operation);
			if (!handleCallAction.Invoke (new MethodProxy (calledMethod), instance, arguments, out MultiValue methodReturnValue)) {
				if (!calledMethod.ReturnsVoid && calledMethod.ReturnType.IsTypeInterestingForDataflow ())
					methodReturnValue = new MethodReturnValue (calledMethod);
				else
					methodReturnValue = TopValue;
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
			var associatedMethod = (IMethodSymbol) Context.OwningSymbol;
			if (associatedMethod.ReturnType.IsTypeInterestingForDataflow ()) {
				var returnParameter = new MethodReturnValue (associatedMethod);

				TrimAnalysisPatterns.Add (
					new TrimAnalysisAssignmentPattern (returnValue, returnParameter, operation),
					isReturnValue: true
				);
			}
		}
	}
}