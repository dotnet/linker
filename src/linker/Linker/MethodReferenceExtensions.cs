﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker
{
	public static class MethodReferenceExtensions
	{
		public static string GetDisplayName (this MethodReference method)
		{
			var sb = new System.Text.StringBuilder ();

			// Append parameters
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count - 1; i++)
					sb.Append (method.Parameters[i].ParameterType.GetDisplayName ()).Append (',');

				sb.Append (method.Parameters[method.Parameters.Count - 1].ParameterType.GetDisplayName ());
			}

			sb.Append (")");

			// Insert generic parameters
			if (method.HasGenericParameters) {
				TypeReferenceExtensions.ParseGenericParameters (method.GenericParameters, null, method.GenericParameters.Count, sb);
			}

			// Insert method name
			if (method.Name == ".ctor")
				sb.Insert (0, method.DeclaringType.Name);
			else
				sb.Insert (0, method.Name);

			// Insert declaring type name
			sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());

			// Insert namespace
			sb.Insert (0, '.').Insert (0, method.GetNamespaceDisplayName ());
			return sb.ToString ();
		}

		public static TypeReference GetReturnType (this MethodReference method)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.ReturnType);

			return method.ReturnType;
		}

		public static TypeReference GetParameterType (this MethodReference method, int parameterIndex)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.Parameters[parameterIndex].ParameterType);

			return method.Parameters[parameterIndex].ParameterType;
		}

		public static bool IsDeclaredOnType (this MethodReference method, string namespaceName, string typeName)
		{
			return method.DeclaringType.IsTypeOf (namespaceName, typeName);
		}

		public static bool HasParameterOfType (this MethodReference method, int parameterIndex, string namespaceName, string typeName)
		{
			return method.Parameters.Count > parameterIndex && method.Parameters[parameterIndex].ParameterType.IsTypeOf (namespaceName, typeName);
		}
	}
}
