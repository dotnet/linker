using System;
using Mono.Cecil;

namespace Mono.Linker
{
	static class MethodReferenceExtensions
	{
		public static TypeReference GetReturnType (this MethodReference method)
		{
			var genericInstance = method.DeclaringType as GenericInstanceType;

			if (genericInstance != null)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.ReturnType);

			return method.ReturnType;
		}

		public static TypeReference GetParameterType (this MethodReference method, int parameterIndex)
		{
			var genericInstance = method.DeclaringType as GenericInstanceType;

			if (genericInstance != null)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.Parameters [parameterIndex].ParameterType);

			return method.Parameters [parameterIndex].ParameterType;
		}
		
		public static bool IsSpecialArrayMethod (this MethodReference method)
		{
			if (!method.DeclaringType.IsArray)
				return false;

			return method.Name == "Set" || method.Name == "Get" || method.Name == "Address" || method.Name == ".ctor";
		}
	}
}
