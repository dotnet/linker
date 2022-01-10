// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial record struct MethodProxy
	{
		public MethodProxy (MethodDefinition method) => Method = method;

		public static implicit operator MethodProxy (MethodDefinition method) => new (method);

		public readonly MethodDefinition Method;

		public string Name { get => Method.Name; }

		internal partial bool IsDeclaredOnType (string namespaceName, string typeName)
			=> Method.IsDeclaredOnType (namespaceName, typeName);

		internal partial bool HasParameters ()
			=> Method.HasParameters;

		internal partial bool HasParametersCount (int parameterCount)
			=> Method.Parameters.Count == parameterCount;

		internal partial bool HasParameterOfType (int parameterIndex, string namespaceName, string typeName)
			=> Method.HasParameterOfType (parameterIndex, namespaceName, typeName);

		internal partial bool HasGenericParameters ()
			=> Method.HasGenericParameters;

		internal partial bool HasGenericParametersCount (int genericParameterCount)
			=> Method.GenericParameters.Count == genericParameterCount;

		internal partial bool IsStatic ()
			=> Method.IsStatic;
	}
}
