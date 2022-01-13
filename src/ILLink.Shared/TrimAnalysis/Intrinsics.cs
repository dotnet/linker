﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct Intrinsics
	{
		public static IntrinsicId GetIntrinsicIdForMethod (MethodProxy calledMethod)
		{
			return calledMethod.Name switch {
				// static System.Reflection.IntrospectionExtensions.GetTypeInfo (Type type)
				"GetTypeInfo" when calledMethod.IsDeclaredOnType ("System.Reflection.IntrospectionExtensions") => IntrinsicId.IntrospectionExtensions_GetTypeInfo,

				// System.Reflection.TypeInfo.AsType ()
				"AsType" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeInfo") => IntrinsicId.TypeInfo_AsType,

				// System.Type.GetTypeInfo (Type type)
				"GetTypeFromHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_GetTypeFromHandle,

				// System.Type.GetTypeHandle (Type type)
				"get_TypeHandle" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_get_TypeHandle,

				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle)
				// System.Reflection.MethodBase.GetMethodFromHandle (RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
				"GetMethodFromHandle" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodBase")
					&& calledMethod.HasParameterOfType (0, "System.RuntimeMethodHandle")
					&& (calledMethod.HasParametersCount (1) || calledMethod.HasParametersCount (2))
					=> IntrinsicId.MethodBase_GetMethodFromHandle,

				// static System.Type.MakeGenericType (Type [] typeArguments)
				"MakeGenericType" when calledMethod.IsDeclaredOnType ("System.Type") => IntrinsicId.Type_MakeGenericType,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeEvent (this Type type, string name)
				"GetRuntimeEvent" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeEvent,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeField (this Type type, string name)
				"GetRuntimeField" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParameterOfType (1, "System.,String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeField,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeMethod (this Type type, string name, Type[] parameters)
				"GetRuntimeMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeMethod,

				// static System.Reflection.RuntimeReflectionExtensions.GetRuntimeProperty (this Type type, string name)
				"GetRuntimeProperty" when calledMethod.IsDeclaredOnType ("System.Reflection.RuntimeReflectionExtensions")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.RuntimeReflectionExtensions_GetRuntimeProperty,

				// static System.Linq.Expressions.Expression.Call (Type, String, Type[], Expression[])
				"Call" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParametersCount (4)
					=> IntrinsicId.Expression_Call,

				// static System.Linq.Expressions.Expression.Field (Expression, Type, String)
				"Field" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType (1, "System.Type")
					&& calledMethod.HasParametersCount (3)
					=> IntrinsicId.Expression_Field,

				// static System.Linq.Expressions.Expression.Property (Expression, Type, String)
				// static System.Linq.Expressions.Expression.Property (Expression, MethodInfo)
				"Property" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& ((calledMethod.HasParameterOfType (1, "System.Type") && calledMethod.HasParametersCount (3))
					|| (calledMethod.HasParameterOfType (1, "System.Reflection.MethodInfo") && calledMethod.HasParametersCount (2)))
					=> IntrinsicId.Expression_Property,

				// static System.Linq.Expressions.Expression.New (Type)
				"New" when calledMethod.IsDeclaredOnType ("System.Linq.Expressions.Expression")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					&& calledMethod.HasParametersCount (1)
					=> IntrinsicId.Expression_New,

				// static System.Type.GetType (string)
				// static System.Type.GetType (string, Boolean)
				// static System.Type.GetType (string, Boolean, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean)
				// static System.Type.GetType (string, Func<AssemblyName, Assembly>, Func<Assembly, String, Boolean, Type>, Boolean, Boolean)
				"GetType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					=> IntrinsicId.Type_GetType,

				// System.Type.GetConstructor (Type[])
				// System.Type.GetConstructor (BindingFlags, Type[])
				// System.Type.GetConstructor (BindingFlags, Binder, Type[], ParameterModifier [])
				// System.Type.GetConstructor (BindingFlags, Binder, CallingConventions, Type[], ParameterModifier [])
				"GetConstructor" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetConstructor,

				// System.Type.GetConstructors (BindingFlags)
				"GetConstructors" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetConstructors,

				// System.Type.GetMethod (string)
				// System.Type.GetMethod (string, BindingFlags)
				// System.Type.GetMethod (string, Type[])
				// System.Type.GetMethod (string, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Type[])
				// System.Type.GetMethod (string, BindingFlags, Binder, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, BindingFlags, Binder, CallingConventions, Type[], ParameterModifier[])
				// System.Type.GetMethod (string, int, Type[])
				// System.Type.GetMethod (string, int, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, Type[], ParameterModifier[]?)
				// System.Type.GetMethod (string, int, BindingFlags, Binder?, CallingConventions, Type[], ParameterModifier[]?)
				"GetMethod" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetMethod,

				// System.Type.GetMethods (BindingFlags)
				"GetMethods" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetMethods,

				// System.Type.GetField (string)
				// System.Type.GetField (string, BindingFlags)
				"GetField" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetField,

				// System.Type.GetFields (BindingFlags)
				"GetFields" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetFields,

				// System.Type.GetEvent (string)
				// System.Type.GetEvent (string, BindingFlags)
				"GetEvent" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetEvent,

				// System.Type.GetEvents (BindingFlags)
				"GetEvents" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetEvents,

				// System.Type.GetNestedType (string)
				// System.Type.GetNestedType (string, BindingFlags)
				"GetNestedType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetNestedType,

				// System.Type.GetNestedTypes (BindingFlags)
				"GetNestedTypes" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetNestedTypes,

				// System.Type.GetMember (String)
				// System.Type.GetMember (String, BindingFlags)
				// System.Type.GetMember (String, MemberTypes, BindingFlags)
				"GetMember" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					&& (calledMethod.HasParametersCount (1) ||
					(calledMethod.HasParametersCount (2) && calledMethod.HasParameterOfType (1, "System.Reflection.BindingFlags")) ||
					(calledMethod.HasParametersCount (3) && calledMethod.HasParameterOfType (2, "System.Reflection.BindingFlags")))
					=> IntrinsicId.Type_GetMember,

				// System.Type.GetMembers (BindingFlags)
				"GetMembers" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetMembers,

				// System.Type.GetInterface (string)
				// System.Type.GetInterface (string, bool)
				"GetInterface" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					&& (calledMethod.HasParametersCount (1) ||
					(calledMethod.HasParametersCount (2) && calledMethod.HasParameterOfType (1, "System.Boolean")))
					=> IntrinsicId.Type_GetInterface,

				// System.Type.AssemblyQualifiedName
				"get_AssemblyQualifiedName" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_AssemblyQualifiedName,

				// System.Type.UnderlyingSystemType
				"get_UnderlyingSystemType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_UnderlyingSystemType,

				// System.Type.BaseType
				"get_BaseType" when calledMethod.IsDeclaredOnType ("System.Type")
					&& !calledMethod.HasParameters ()
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_get_BaseType,

				// System.Type.GetProperty (string)
				// System.Type.GetProperty (string, BindingFlags)
				// System.Type.GetProperty (string, Type)
				// System.Type.GetProperty (string, Type[])
				// System.Type.GetProperty (string, Type, Type[])
				// System.Type.GetProperty (string, Type, Type[], ParameterModifier[])
				// System.Type.GetProperty (string, BindingFlags, Binder, Type, Type[], ParameterModifier[])
				"GetProperty" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetProperty,

				// System.Type.GetProperties (BindingFlags)
				"GetProperties" when calledMethod.IsDeclaredOnType ("System.Type")
					&& calledMethod.HasParameterOfType (0, "System.Reflection.BindingFlags")
					&& calledMethod.HasParametersCount (1)
					&& !calledMethod.IsStatic ()
					=> IntrinsicId.Type_GetProperties,

				// static System.Object.GetType ()
				"GetType" when calledMethod.IsDeclaredOnType ("System.Object")
					=> IntrinsicId.Object_GetType,

				".ctor" when calledMethod.IsDeclaredOnType ("System.Reflection.TypeDelegator")
					&& calledMethod.HasParameterOfType (0, "System.Type")
					=> IntrinsicId.TypeDelegator_Ctor,

				"Empty" when calledMethod.IsDeclaredOnType ("System.Array")
					=> IntrinsicId.Array_Empty,

				// static System.Activator.CreateInstance (System.Type type)
				// static System.Activator.CreateInstance (System.Type type, bool nonPublic)
				// static System.Activator.CreateInstance (System.Type type, params object?[]? args)
				// static System.Activator.CreateInstance (System.Type type, object?[]? args, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture)
				// static System.Activator.CreateInstance (System.Type type, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes) { throw null; }
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType (0, "System.Type")
					=> IntrinsicId.Activator_CreateInstance_Type,

				// static System.Activator.CreateInstance (string assemblyName, string typeName)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object?[]? args, System.Globalization.CultureInfo? culture, object?[]? activationAttributes)
				// static System.Activator.CreateInstance (string assemblyName, string typeName, object?[]? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.Activator_CreateInstance_AssemblyName_TypeName,

				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// static System.Activator.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& !calledMethod.HasGenericParameters ()
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.Activator_CreateInstanceFrom,

				// static T System.Activator.CreateInstance<T> ()
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Activator")
					&& calledMethod.HasGenericParameters ()
					&& calledMethod.HasGenericParametersCount (1)
					&& calledMethod.HasParametersCount (0)
					=> IntrinsicId.Activator_CreateInstanceOfT,

				// System.AppDomain.CreateInstance (string assemblyName, string typeName)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstance (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.AppDomain_CreateInstance,

				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceAndUnwrap (string assemblyName, string typeName, object? []? activationAttributes)
				"CreateInstanceAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceAndUnwrap,

				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFrom (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFrom" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFrom,

				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, bool ignoreCase, System.Reflection.BindingFlags bindingAttr, System.Reflection.Binder? binder, object? []? args, System.Globalization.CultureInfo? culture, object? []? activationAttributes)
				// System.AppDomain.CreateInstanceFromAndUnwrap (string assemblyFile, string typeName, object? []? activationAttributes)
				"CreateInstanceFromAndUnwrap" when calledMethod.IsDeclaredOnType ("System.AppDomain")
					&& calledMethod.HasParameterOfType (0, "System.String")
					&& calledMethod.HasParameterOfType (1, "System.String")
					=> IntrinsicId.AppDomain_CreateInstanceFromAndUnwrap,

				// System.Reflection.Assembly.CreateInstance (string typeName)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase)
				// System.Reflection.Assembly.CreateInstance (string typeName, bool ignoreCase, BindingFlags bindingAttr, Binder? binder, object []? args, CultureInfo? culture, object []? activationAttributes)
				"CreateInstance" when calledMethod.IsDeclaredOnType ("System.Reflection.Assembly")
					&& calledMethod.HasParameterOfType (0, "System.String")
					=> IntrinsicId.Assembly_CreateInstance,

				// System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor (RuntimeTypeHandle type)
				"RunClassConstructor" when calledMethod.IsDeclaredOnType ("System.Runtime.CompilerServices.RuntimeHelpers")
					&& calledMethod.HasParameterOfType (0, "System.RuntimeTypeHandle")
					=> IntrinsicId.RuntimeHelpers_RunClassConstructor,

				// System.Reflection.MethodInfo.MakeGenericMethod (Type[] typeArguments)
				"MakeGenericMethod" when calledMethod.IsDeclaredOnType ("System.Reflection.MethodInfo")
					&& !calledMethod.IsStatic ()
					&& calledMethod.HasParametersCount (1)
					=> IntrinsicId.MethodInfo_MakeGenericMethod,

				_ => IntrinsicId.None,
			};
		}

		public bool HandleMethodCall (MethodProxy calledMethod, MultiValue instanceValue, IReadOnlyList<MultiValue> argumentValues, out MultiValue methodReturnValue)
		{
			methodReturnValue = new MultiValue ();

			bool requiresDataFlowAnalysis = MethodRequiresDataFlowAnalysis (calledMethod);
			DynamicallyAccessedMemberTypes returnValueDynamicallyAccessedMemberTypes = requiresDataFlowAnalysis ?
				GetReturnValueAnnotation (calledMethod) : 0;

			switch (GetIntrinsicIdForMethod (calledMethod)) {
			case IntrinsicId.IntrospectionExtensions_GetTypeInfo:
				Debug.Assert (instanceValue.IsEmpty ());
				Debug.Assert (argumentValues.Count == 1);

				// typeof(Foo).GetTypeInfo()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				methodReturnValue = argumentValues[0];
				break;

			case IntrinsicId.TypeInfo_AsType:
				// someType.AsType()... will be commonly present in code targeting
				// the dead-end reflection refactoring. The call doesn't do anything and we
				// don't want to lose the annotation.
				methodReturnValue = instanceValue;
				break;

			default:
				return false;
			}

			// If we get here, we handled this as an intrinsic.  As a convenience, if the code above
			// didn't set the return value (and the method has a return value), we will set it to be an
			// unknown value with the return type of the method.
			if (methodReturnValue.IsEmpty ()) {
				if (!calledMethod.GetReturnType ().IsVoid ()) {
					methodReturnValue = GetMethodReturnValue (calledMethod, returnValueDynamicallyAccessedMemberTypes);
				}
			}

			// Validate that the return value has the correct annotations as per the method return value annotations
			if (returnValueDynamicallyAccessedMemberTypes != 0) {
				foreach (var uniqueValue in methodReturnValue) {
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
		}

		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes);

		// Only used for internal diagnostic purposes (not even for warning messages)
		private partial string GetContainingSymbolDisplayName ();
	}
}
