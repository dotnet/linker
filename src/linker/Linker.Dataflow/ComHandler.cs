// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	static class ComHandler
	{
		static bool IsComInterop (IMarshalInfoProvider marshalInfoProvider, TypeReference parameterType, LinkContext context)
		{
			// This is best effort. One can likely find ways how to get COM without triggering these alarms.
			// AsAny marshalling of a struct with an object-typed field would be one, for example.

			// This logic roughly corresponds to MarshalInfo::MarshalInfo in CoreCLR,
			// not trying to handle invalid cases and distinctions that are not interesting wrt
			// "is this COM?" question.

			NativeType nativeType = NativeType.None;
			if (marshalInfoProvider.HasMarshalInfo) {
				nativeType = marshalInfoProvider.MarshalInfo.NativeType;
			}

			if (nativeType == NativeType.IUnknown || nativeType == NativeType.IDispatch || nativeType == NativeType.IntF) {
				// This is COM by definition
				return true;
			}

			if (nativeType == NativeType.None) {
				// Resolve will look at the element type
				var parameterTypeDef = context.TryResolve (parameterType);

				if (parameterTypeDef != null) {
					if (parameterTypeDef.IsTypeOf (WellKnownType.System_Array)) {
						// System.Array marshals as IUnknown by default
						return true;
					} else if (parameterTypeDef.IsTypeOf (WellKnownType.System_String) ||
						parameterTypeDef.IsTypeOf ("System.Text", "StringBuilder")) {
						// String and StringBuilder are special cased by interop
						return false;
					}

					if (parameterTypeDef.IsValueType) {
						// Value types don't marshal as COM
						return false;
					} else if (parameterTypeDef.IsInterface) {
						// Interface types marshal as COM by default
						return true;
					} else if (parameterTypeDef.IsMulticastDelegate ()) {
						// Delegates are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "CriticalHandle", context)) {
						// Subclasses of CriticalHandle are special cased by interop
						return false;
					} else if (parameterTypeDef.IsSubclassOf ("System.Runtime.InteropServices", "SafeHandle", context)) {
						// Subclasses of SafeHandle are special cased by interop
						return false;
					} else if (!parameterTypeDef.IsSequentialLayout && !parameterTypeDef.IsExplicitLayout) {
						// Rest of classes that don't have layout marshal as COM
						return true;
					}
				}
			}

			return false;
		}

		public static bool ComDangerousMethod (MethodDefinition methodDefinition, LinkContext context)
		{
			bool comDangerousMethod = IsComInterop (methodDefinition.MethodReturnType, methodDefinition.ReturnType, context);
			foreach (ParameterDefinition pd in methodDefinition.Parameters) {
				comDangerousMethod |= IsComInterop (pd, pd.ParameterType, context);
			}

			return comDangerousMethod;
		}
	}
}
