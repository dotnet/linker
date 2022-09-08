// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	public partial record ParameterReferenceValue (MethodDefinition MethodDefinition, ILParameterIndex ParameterIndex)
		: ReferenceValue (MethodDefinition.GetParameterType (ParameterIndex))
	{
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
}
