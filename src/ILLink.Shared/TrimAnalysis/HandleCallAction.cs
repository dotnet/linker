﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	[StructLayout (LayoutKind.Auto)] // A good way to avoid CS0282, we don't really care about field order
	partial struct HandleCallAction
	{
		static ValueSetLattice<SingleValue> MultiValueLattice => default;

		readonly RequireDynamicallyAccessedMembersAction _requireDynamicallyAccessedMembersAction;

		public bool Invoke (MethodProxy calledMethod, MultiValue instanceValue, IReadOnlyList<MultiValue> argumentValues, out MultiValue methodReturnValue)
		{
			MultiValue? returnValue;

			bool requiresDataFlowAnalysis = MethodRequiresDataFlowAnalysis (calledMethod);
			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
				GetReturnValueAnnotation (calledMethod) : 0;

			switch (Intrinsics.GetIntrinsicIdForMethod (calledMethod)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
				Debug.Assert (instanceValue.IsEmpty ());
				Debug.Assert (argumentValues.Count == 1);

				// typeof(Foo).GetTypeInfo()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				returnValue = argumentValues[0];
				break;

			case IntrinsicId.TypeInfo_AsType:
				// someType.AsType()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				returnValue = instanceValue;
				break;

			//
			// UnderlyingSystemType
			//
			case IntrinsicId.Type_get_UnderlyingSystemType:
				// This is identity for the purposes of the analysis.
				returnValue = instanceValue;
				break;

			case IntrinsicId.Type_GetTypeFromHandle:
				// Infrastructure piece to support "typeof(Foo)" in IL and direct calls everywhere
				InitReturnValue ();
				foreach (var value in argumentValues[0]) {
					if (value is RuntimeTypeHandleValue typeHandle)
						AddReturnValue (new SystemTypeValue (typeHandle.RepresentedType));
					else if (value is RuntimeTypeHandleForGenericParameterValue typeHandleForGenericParameter)
						AddReturnValue (GetGenericParameterValue (typeHandleForGenericParameter.GenericParameter));
					else if (value == NullValue.Instance)
						AddReturnValue (value);
					else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			case IntrinsicId.Type_get_TypeHandle:
				InitReturnValue ();
				foreach (var value in instanceValue) {
					if (value is SystemTypeValue typeValue)
						AddReturnValue (new RuntimeTypeHandleValue (typeValue.RepresentedType));
					else if (value is GenericParameterValue genericParameterValue)
						AddReturnValue (new RuntimeTypeHandleForGenericParameterValue (genericParameterValue.GenericParameter));
					else if (value == NullValue.Instance) {
						// Skip the null here - the method would throw if this happens at runtime, so there's no return value
					} else
						AddReturnValue (GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes));
				}
				break;

			//
			// GetInterface (String)
			// GetInterface (String, bool)
			//
			case IntrinsicId.Type_GetInterface: {
					InitReturnValue ();
					var targetValue = GetMethodThisParameterValue (calledMethod, DynamicallyAccessedMemberTypesOverlay.Interfaces);
					foreach (var value in instanceValue) {
						// For now no support for marking a single interface by name. We would have to correctly support
						// mangled names for generics to do that correctly. Simply mark all interfaces on the type for now.

						// Require Interfaces annotation
						_requireDynamicallyAccessedMembersAction.Invoke (value, targetValue);

						// Interfaces is transitive, so the return values will always have at least Interfaces annotation
						DynamicallyAccessedMemberTypes returnMemberTypes = DynamicallyAccessedMemberTypesOverlay.Interfaces;

						// Propagate All annotation across the call - All is a superset of Interfaces
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers
							&& valueWithDynamicallyAccessedMembers.DynamicallyAccessedMemberTypes == DynamicallyAccessedMemberTypes.All)
							returnMemberTypes = DynamicallyAccessedMemberTypes.All;

						AddReturnValue (GetMethodReturnValue (calledMethod, returnMemberTypes));
					}
				}
				break;

			//
			// AssemblyQualifiedName
			//
			case IntrinsicId.Type_get_AssemblyQualifiedName: {
					InitReturnValue ();
					foreach (var value in instanceValue) {
						if (value is ValueWithDynamicallyAccessedMembers valueWithDynamicallyAccessedMembers) {
							// Currently we don't need to track the difference between Type and String annotated values
							// that only matters when we use them, so Type.GetType is the difference really.
							// For diagnostics we actually don't want to track the Type.AssemblyQualifiedName
							// as the annotation does not come from that call, but from its input.
							AddReturnValue (valueWithDynamicallyAccessedMembers);
						} else if (value == NullValue.Instance) {
							// Skip the null here - the method would throw if this happens at runtime, so there's no return value
						} else {
							AddReturnValue (UnknownValue.Instance);
						}
					}
				}
				break;

			// Disable Type_GetMethod, Type_GetProperty, Type_GetField, Type_GetConstructor, Type_GetEvent, Activator_CreateInstance_Type
			// These calls have annotations on the runtime by default, trying to analyze the annotations without intrinsic handling
			// might end up generating unnecessary warnings. So we disable handling these calls until a proper intrinsic handling is made
			case IntrinsicId.Type_GetMethod:
			case IntrinsicId.Type_GetProperty:
			case IntrinsicId.Type_GetField:
			case IntrinsicId.Type_GetConstructor:
			case IntrinsicId.Type_GetEvent:
			case IntrinsicId.Activator_CreateInstance_Type:
				methodReturnValue = MultiValueLattice.Top;
				return true;

			default:
				methodReturnValue = MultiValueLattice.Top;
				return false;
			}

			if (!returnValue.HasValue) {
				// All methods which can return value should call InitReturnValue - this is to make sure that the intrinsics are aware of the need
				// to set return value correctly.
				Debug.Assert (calledMethod.ReturnsVoid ());
				returnValue = MultiValueLattice.Top;
			}

			methodReturnValue = returnValue.Value;

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in returnValue) {
					if (uniqueValue is ValueWithDynamicallyAccessedMembers methodReturnValueWithMemberTypes) {
						if (!methodReturnValueWithMemberTypes.DynamicallyAccessedMemberTypes.HasFlag (returnValueDynamicallyAccessedMemberTypes))
							throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					} else if (uniqueValue is SystemTypeValue) {
						// SystemTypeValue can fullfill any requirement, so it's always valid
						// The requirements will be applied at the point where it's consumed (passed as a method parameter, set as field value, returned from the method)
					} else {
						throw new InvalidOperationException ($"Internal linker error: in {GetContainingSymbolDisplayName ()} processing call to {calledMethod.GetDisplayName ()} returned value which is not correctly annotated with the expected dynamic member access kinds.");
					}
				}
			}

			return true;

			void InitReturnValue () => returnValue = MultiValueLattice.Top;
			void AddReturnValue (MultiValue value)
			{
				Debug.Assert (returnValue.HasValue); // You should call InitReturnValue if the intrinsic is supposed to return a value
				returnValue = returnValue.HasValue ? MultiValueLattice.Meet (returnValue.Value, value) : value;
			}
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		private partial GenericParameterValue GetGenericParameterValue (GenericParameterProxy genericParameter);

		private partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		// Only used for internal diagnostic purposes (not even for warning messages)
		private partial string GetContainingSymbolDisplayName ();
	}
}
