﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial struct MethodProxy
	{
		public MethodProxy (MethodDefinition method) => Method = method;

		public static implicit operator MethodProxy (MethodDefinition method) => new (method);

		public readonly MethodDefinition Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => Method.IsDeclaredOnType (fullTypeName);

		internal partial bool HasParameters () => Method.HasParameters;

		internal partial bool HasParametersCount (int parameterCount) => Method.Parameters.Count == parameterCount;

		internal partial bool HasParameterOfType (int parameterIndex, string fullTypeName) => Method.HasParameterOfType (parameterIndex, fullTypeName);

		internal partial bool HasGenericParameters () => Method.HasGenericParameters;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.GenericParameters.Count == genericParameterCount;

		internal partial bool IsStatic () => Method.IsStatic;

		/// <summary>
		/// Uses the logic from <see cref="MethodBodyScanner.GetReturnTypeWithoutModifiers(TypeReference)"/> to get the
		/// return type and determine if it is void
		/// </summary>
		internal partial bool ReturnsVoid ()
		{
			var returnType = Method.ReturnType;
			while (returnType is IModifierType) {
				returnType = ((IModifierType) returnType).ElementType;
			}
			return returnType.MetadataType == MetadataType.Void;
		}

		public override string ToString () => Method.ToString ();
	}
}
