// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial record struct MethodProxy
	{
		public MethodProxy (IMethodSymbol method) => Method = method;

		public readonly IMethodSymbol Method;

		public string Name { get => Method.Name; }

		internal partial bool IsDeclaredOnType (string namespaceName, string typeName)
			=> IsTypeOf (Method.ContainingType, namespaceName, typeName);

		internal partial bool HasParameters ()
			=> Method.Parameters.Length > 0;

		internal partial bool HasParametersCount (int parameterCount)
			=> Method.Parameters.Length == parameterCount;

		internal partial bool HasParameterOfType (int parameterIndex, string namespaceName, string typeName)
			=> Method.Parameters.Length > parameterIndex && IsTypeOf (Method.Parameters[parameterIndex].Type, namespaceName, typeName);

		internal partial bool HasGenericParameters ()
			=> Method.IsGenericMethod;

		internal partial bool HasGenericParametersCount (int genericParameterCount)
			=> Method.TypeParameters.Length == genericParameterCount;

		internal partial bool IsStatic ()
			=> Method.IsStatic;

		bool IsTypeOf (ITypeSymbol type, string namespaceName, string typeName)
		{
			if (type is not INamedTypeSymbol namedType)
				return false;

			var remainingNamespaceName = namespaceName.AsSpan ();
			var containingNamespace = namedType.ContainingNamespace;
			while (containingNamespace != null) {
				var actualNamespaceName = containingNamespace.Name.AsSpan ();

				if (containingNamespace.ContainingNamespace.IsGlobalNamespace) {
					if (remainingNamespaceName.Equals (actualNamespaceName, StringComparison.Ordinal))
						break;

					return false;
				}

				if (remainingNamespaceName.Length < actualNamespaceName.Length + 1)
					return false;

				var expectedNamespaceName = remainingNamespaceName.Slice (remainingNamespaceName.Length - actualNamespaceName.Length);
				if (!expectedNamespaceName.Equals (actualNamespaceName, StringComparison.Ordinal) ||
					remainingNamespaceName[remainingNamespaceName.Length - actualNamespaceName.Length - 1] != '.')
					return false;

				containingNamespace = containingNamespace.ContainingNamespace;
				remainingNamespaceName = remainingNamespaceName.Slice (0, remainingNamespaceName.Length - actualNamespaceName.Length - 1);
			}

			return namedType.Name == typeName;
		}
	}
}
