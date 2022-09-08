// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	static class DiagnosticUtilities
	{
		internal static IMetadataTokenProvider GetMethodParameterFromIndex (MethodDefinition method, ILParameterIndex parameterIndex)
		{
			if (method.IsImplicitThisParameter (parameterIndex))
				return method;
			return method.GetParameter (parameterIndex);
		}

		internal static string GetParameterNameForErrorMessage (ParameterDefinition parameterDefinition) =>
			string.IsNullOrEmpty (parameterDefinition.Name) ? $"#{parameterDefinition.Index}" : parameterDefinition.Name;

		internal static string GetGenericParameterDeclaringMemberDisplayName (GenericParameter genericParameter) =>
			genericParameter.DeclaringMethod != null ?
				genericParameter.DeclaringMethod.GetDisplayName () :
				genericParameter.DeclaringType.GetDisplayName ();

		internal static string GetMethodSignatureDisplayName (IMethodSignature methodSignature) =>
			(methodSignature is MethodReference method) ? method.GetDisplayName () : (methodSignature.ToString () ?? string.Empty);
	}
}
