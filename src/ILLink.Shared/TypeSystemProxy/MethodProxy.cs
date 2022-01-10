// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial record struct MethodProxy : IMemberProxy
	{
		internal partial bool IsDeclaredOnType (string namespaceName, string typeName);
		internal partial bool HasParameters ();
		internal partial bool HasParametersCount (int parameterCount);
		internal partial bool HasParameterOfType (int parameterIndex, string namespaceName, string typeName);
		internal partial bool HasGenericParameters ();
		internal partial bool HasGenericParametersCount (int genericParameterCount);
		internal partial bool IsStatic ();
	}
}
