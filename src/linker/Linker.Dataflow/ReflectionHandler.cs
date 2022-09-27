﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker.Steps;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	sealed class ReflectionHandler
	{
		private MessageOrigin _origin;
		private readonly LinkContext _context;
		private readonly ReflectionMarker _reflectionMarker;
		private readonly FlowAnnotations _annotations;
		private readonly MarkStep _markStep;
		private readonly TrimAnalysisPatternStore _trimAnalysisPatternStore;
		private static ValueSetLatticeWithUnknownValue<SingleValue> MultiValueLattice => default;


		public ReflectionHandler (LinkContext context, MarkStep parent, MessageOrigin origin, TrimAnalysisPatternStore trimAnalysisPatternStore)
		{
			_context = context;
			_markStep = parent;
			_origin = origin;
			_reflectionMarker = new ReflectionMarker (context, parent, enabled: false);
			_annotations = context.Annotations.FlowAnnotations;
			_trimAnalysisPatternStore = trimAnalysisPatternStore;

		}

		public void HandleStoreField (FieldValue field, Instruction operation, MultiValue valueToStore)
			=> HandleStoreValueWithDynamicallyAccessedMembers (field, operation, valueToStore);

		public void HandleStoreParameter (MethodParameterValue parameter, Instruction operation, MultiValue valueToStore)
			=> HandleStoreValueWithDynamicallyAccessedMembers (parameter, operation, valueToStore);

		public void HandleStoreMethodThisParameter (MethodThisParameterValue thisParameter, Instruction operation, MultiValue valueToStore)
			=> HandleStoreValueWithDynamicallyAccessedMembers (thisParameter, operation, valueToStore);

		public void HandleStoreMethodReturnValue (MethodReturnValue returnValue, Instruction operation, MultiValue valueToStore)
			=> HandleStoreValueWithDynamicallyAccessedMembers (returnValue, operation, valueToStore);

		public bool HandleCall (MethodBody callingMethodBody, MethodReference calledMethod, Instruction operation, ValueNodeList methodParams, out MultiValue methodReturnValue)
		{
			methodReturnValue = new ();

			var reflectionProcessed = _markStep.ProcessReflectionDependency (callingMethodBody, operation);
			if (reflectionProcessed)
				return false;

			Debug.Assert (callingMethodBody.Method == _origin.Provider);
			var calledMethodDefinition = _context.TryResolve (calledMethod);
			if (calledMethodDefinition == null)
				return false;

			_origin = _origin.WithInstructionOffset (operation.Offset);

			MultiValue instanceValue;
			ImmutableArray<MultiValue> arguments;
			if (calledMethodDefinition.HasImplicitThis ()) {
				instanceValue = methodParams[0];
				arguments = methodParams.Skip (1).ToImmutableArray ();
			} else {
				instanceValue = MultiValueLattice.Top;
				arguments = methodParams.ToImmutableArray ();
			}

			_trimAnalysisPatternStore.Add (new TrimAnalysisMethodCallPattern (
				operation,
				calledMethod,
				instanceValue,
				arguments,
				_origin
			));

			var diagnosticContext = new DiagnosticContext (_origin, diagnosticsEnabled: false, _context);
			return HandleCall (
				operation,
				calledMethod,
				instanceValue,
				arguments,
				diagnosticContext,
				_reflectionMarker,
				_context,
				_markStep,
				out methodReturnValue);
		}

		public static bool HandleCall (
			Instruction operation,
			MethodReference calledMethod,
			MultiValue instanceValue,
			ImmutableArray<MultiValue> argumentValues,
			DiagnosticContext diagnosticContext,
			ReflectionMarker reflectionMarker,
			LinkContext context,
			MarkStep markStep,
			out MultiValue methodReturnValue)
		{
			var origin = diagnosticContext.Origin;
			var calledMethodDefinition = context.TryResolve (calledMethod);
			Debug.Assert (calledMethodDefinition != null);
			var callingMethodDefinition = origin.Provider as MethodDefinition;
			Debug.Assert (callingMethodDefinition != null);

			bool requiresDataFlowAnalysis = context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (calledMethodDefinition);
			var annotatedMethodReturnValue = context.Annotations.FlowAnnotations.GetMethodReturnValue (calledMethodDefinition);
			Debug.Assert (requiresDataFlowAnalysis || annotatedMethodReturnValue.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.None);

			MultiValue? maybeMethodReturnValue = null;

			var handleCallAction = new HandleCallAction (context, reflectionMarker, diagnosticContext, callingMethodDefinition);
			switch (Intrinsics.GetIntrinsicIdForMethod (calledMethodDefinition)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
			case IntrinsicId.TypeInfo_AsType:
			case IntrinsicId.Type_get_UnderlyingSystemType:
			case IntrinsicId.Type_GetTypeFromHandle:
			case IntrinsicId.Type_get_TypeHandle:
			case IntrinsicId.Type_GetInterface:
			case IntrinsicId.Type_get_AssemblyQualifiedName:
			case IntrinsicId.RuntimeHelpers_RunClassConstructor:
			case var callType when (callType == IntrinsicId.Type_GetConstructors || callType == IntrinsicId.Type_GetMethods || callType == IntrinsicId.Type_GetFields ||
				callType == IntrinsicId.Type_GetProperties || callType == IntrinsicId.Type_GetEvents || callType == IntrinsicId.Type_GetNestedTypes || callType == IntrinsicId.Type_GetMembers)
				&& calledMethod.DeclaringType.IsTypeOf (WellKnownType.System_Type)
				&& calledMethod.Parameters[0].ParameterType.FullName == "System.Reflection.BindingFlags"
				&& calledMethod.HasThis:
			case var fieldPropertyOrEvent when (fieldPropertyOrEvent == IntrinsicId.Type_GetField || fieldPropertyOrEvent == IntrinsicId.Type_GetProperty || fieldPropertyOrEvent == IntrinsicId.Type_GetEvent)
				&& calledMethod.DeclaringType.IsTypeOf (WellKnownType.System_Type)
				&& calledMethod.Parameters[0].ParameterType.IsTypeOf (WellKnownType.System_String)
				&& calledMethod.HasThis:
			case var getRuntimeMember when getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod
				|| getRuntimeMember == IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty:
			case IntrinsicId.Type_GetMember:
			case IntrinsicId.Type_GetMethod:
			case IntrinsicId.Type_GetNestedType:
			case IntrinsicId.Nullable_GetUnderlyingType:
			case IntrinsicId.Expression_Property when calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo"):
			case var fieldOrPropertyIntrinsic when fieldOrPropertyIntrinsic == IntrinsicId.Expression_Field || fieldOrPropertyIntrinsic == IntrinsicId.Expression_Property:
			case IntrinsicId.Type_get_BaseType:
			case IntrinsicId.Type_GetConstructor:
			case IntrinsicId.MethodBase_GetMethodFromHandle:
			case IntrinsicId.MethodBase_get_MethodHandle:
			case IntrinsicId.Type_MakeGenericType:
			case IntrinsicId.MethodInfo_MakeGenericMethod:
			case IntrinsicId.Expression_Call:
			case IntrinsicId.Expression_New:
			case IntrinsicId.Type_GetType:
			case IntrinsicId.Activator_CreateInstance_Type:
			case IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName:
			case IntrinsicId.Activator_CreateInstanceFrom:
			case var appDomainCreateInstance when appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstance
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceAndUnwrap
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFrom
					|| appDomainCreateInstance == IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap:
			case IntrinsicId.Assembly_CreateInstance: {
					return handleCallAction.Invoke (calledMethodDefinition, instanceValue, argumentValues, out methodReturnValue, out _);
				}

			case IntrinsicId.None: {
					if (calledMethodDefinition.IsPInvokeImpl && ComHandler.ComDangerousMethod (calledMethodDefinition, context))
						diagnosticContext.AddDiagnostic (DiagnosticId.CorrectnessOfCOMCannotBeGuaranteed, calledMethodDefinition.GetDisplayName ());
					if (context.Annotations.DoesMethodRequireUnreferencedCode (calledMethodDefinition, out RequiresUnreferencedCodeAttribute? requiresUnreferencedCode))
						MarkStep.ReportRequiresUnreferencedCode (calledMethodDefinition.GetDisplayName (), requiresUnreferencedCode, diagnosticContext);

					return handleCallAction.Invoke (calledMethodDefinition, instanceValue, argumentValues, out methodReturnValue, out _);
				}

			case IntrinsicId.TypeDelegator_Ctor: {
					// This is an identity function for analysis purposes
					if (operation.OpCode == OpCodes.Newobj)
						AddReturnValue (argumentValues[0]);
				}
				break;

			case IntrinsicId.Array_Empty: {
					AddReturnValue (ArrayValue.Create (0, ((GenericInstanceMethod) calledMethod).GenericArguments[0]));
				}
				break;

			case IntrinsicId.Enum_GetValues:
			case IntrinsicId.Marshal_SizeOf:
			case IntrinsicId.Marshal_OffsetOf:
			case IntrinsicId.Marshal_PtrToStructure:
			case IntrinsicId.Marshal_DestroyStructure:
			case IntrinsicId.Marshal_GetDelegateForFunctionPointer:
				// These intrinsics are not interesting for trimmer (they are interesting for AOT and that's why they are recognized)
				break;

			//
			// System.Object
			//
			// GetType()
			//
			case IntrinsicId.Object_GetType: {
					foreach (var valueNode in instanceValue) {
						// Note that valueNode can be statically typed in IL as some generic argument type.
						// For example:
						//   void Method<T>(T instance) { instance.GetType().... }
						// Currently this case will end up with null StaticType - since there's no typedef for the generic argument type.
						// But it could be that T is annotated with for example PublicMethods:
						//   void Method<[DAM(PublicMethods)] T>(T instance) { instance.GetType().GetMethod("Test"); }
						// In this case it's in theory possible to handle it, by treating the T basically as a base class
						// for the actual type of "instance". But the analysis for this would be pretty complicated (as the marking
						// has to happen on the callsite, which doesn't know that GetType() will be used...).
						// For now we're intentionally ignoring this case - it will produce a warning.
						// The counter example is:
						//   Method<Base>(new Derived);
						// In this case to get correct results, trimmer would have to mark all public methods on Derived. Which
						// currently it won't do.

						TypeDefinition? staticType = (valueNode as IValueWithStaticType)?.StaticType;
						if (staticType is null) {
							// We don't know anything about the type GetType was called on. Track this as a usual result of a method call without any annotations
							AddReturnValue (context.Annotations.FlowAnnotations.GetMethodReturnValue (calledMethodDefinition));
						} else if (staticType.IsSealed || staticType.IsTypeOf ("System", "Delegate")) {
							// We can treat this one the same as if it was a typeof() expression

							// We can allow Object.GetType to be modeled as System.Delegate because we keep all methods
							// on delegates anyway so reflection on something this approximation would miss is actually safe.

							// We ignore the fact that the type can be annotated (see below for handling of annotated types)
							// This means the annotations (if any) won't be applied - instead we rely on the exact knowledge
							// of the type. So for example even if the type is annotated with PublicMethods
							// but the code calls GetProperties on it - it will work - mark properties, don't mark methods
							// since we ignored the fact that it's annotated.
							// This can be seen a little bit as a violation of the annotation, but we already have similar cases
							// where a parameter is annotated and if something in the method sets a specific known type to it
							// we will also make it just work, even if the annotation doesn't match the usage.
							AddReturnValue (new SystemTypeValue (staticType));
						} else {
							// Make sure the type is marked (this will mark it as used via reflection, which is sort of true)
							// This should already be true for most cases (method params, fields, ...), but just in case
							reflectionMarker.MarkType (origin, staticType);

							var annotation = markStep.DynamicallyAccessedMembersTypeHierarchy
								.ApplyDynamicallyAccessedMembersToTypeHierarchy (staticType);

							// Return a value which is "unknown type" with annotation. For now we'll use the return value node
							// for the method, which means we're loosing the information about which staticType this
							// started with. For now we don't need it, but we can add it later on.
							AddReturnValue (context.Annotations.FlowAnnotations.GetMethodReturnValue (calledMethodDefinition, annotation));
						}
					}
				}
				break;

			// Note about Activator.CreateInstance<T>
			// There are 2 interesting cases:
			//  - The generic argument for T is either specific type or annotated - in that case generic instantiation will handle this
			//    since from .NET 6+ the T is annotated with PublicParameterlessConstructor annotation, so the linker would apply this as for any other method.
			//  - The generic argument for T is unannotated type - the generic instantiantion handling has a special case for handling PublicParameterlessConstructor requirement
			//    in such that if the generic argument type has the "new" constraint it will not warn (as it is effectively the same thing semantically).
			//    For all other cases, the linker would have already produced a warning.

			default:
				throw new NotImplementedException ("Unhandled intrinsic");
			}

			// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
			// didn't set the return value (and the method has a return value), we will set it to be an
			// unknown value with the return type of the method.
			bool returnsVoid = calledMethod.ReturnsVoid ();
			methodReturnValue = maybeMethodReturnValue ?? (returnsVoid ?
				MultiValueLattice.Top :
				annotatedMethodReturnValue);

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (annotatedMethodReturnValue.DynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in methodReturnValue) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (annotatedMethodReturnValue.DynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fulfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else {
						throw new InvalidOperationException ($"Internal linker error: processing of call from {callingMethodDefinition.GetDisplayName ()} to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			return true;

			void AddReturnValue (MultiValue value)
			{
				maybeMethodReturnValue = (maybeMethodReturnValue is null) ? value : MultiValueLattice.Meet ((MultiValue) maybeMethodReturnValue, value);
			}
		}

		public MultiValue GetFieldValue (FieldDefinition field) => _annotations.GetFieldValue (field);

		private void HandleStoreValueWithDynamicallyAccessedMembers (ValueWithDynamicallyAccessedMembers targetValue, Instruction operation, MultiValue sourceValue)
		{
			if (targetValue.DynamicallyAccessedMemberTypes != 0) {
				_origin = _origin.WithInstructionOffset (operation.Offset);
				HandleAssignmentPattern (sourceValue, targetValue);
			}
		}

		public void HandleAssignmentPattern (
			in MultiValue value,
			ValueWithDynamicallyAccessedMembers targetValue)
		{
			_trimAnalysisPatternStore.Add (new TrimAnalysisAssignmentPattern (value, targetValue, _origin));
		}

		public ValueWithDynamicallyAccessedMembers GetMethodThisParameterValue (MethodDefinition method)
			=> _annotations.GetMethodThisParameterValue (method);

		public ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, SourceParameterIndex parameterIndex)
			=> GetMethodParameterValue (method, parameterIndex, _annotations.GetParameterAnnotation (method, parameterIndex));

		ValueWithDynamicallyAccessedMembers GetMethodParameterValue (MethodDefinition method, SourceParameterIndex parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			return _annotations.GetMethodParameterValue (method, parameterIndex, dynamicallyAccessedMemberTypes);
		}

		public static void WarnAboutInvalidILInMethod (MethodBody method, int ilOffset)
		{
			// Serves as a debug helper to make sure valid IL is not considered invalid.
			//
			// The .NET Native compiler used to warn if it detected invalid IL during treeshaking,
			// but the warnings were often triggered in autogenerated dead code of a major game engine
			// and resulted in support calls. No point in warning. If the code gets exercised at runtime,
			// an InvalidProgramException will likely be raised.
			Debug.WriteLine ($"{method}, IL{ilOffset}: Invalid IL detected");
			Debug.Fail ("Invalid IL or a bug in the scanner");
		}

		public void HandleReturnValue (MethodDefinition method, MultiValue returnValue)
		{
			if (!method.ReturnsVoid ()) {
				var methodReturnValue = _annotations.GetMethodReturnValue (method);
				if (methodReturnValue.DynamicallyAccessedMemberTypes != 0)
					HandleAssignmentPattern (returnValue, methodReturnValue);
			}
		}
	}
}
