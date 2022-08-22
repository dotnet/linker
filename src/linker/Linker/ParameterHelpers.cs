// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;
using Mono.Cecil.Cil;
using ILLink.Shared;

namespace Mono.Linker
{
	public static class ParameterHelpers
	{
		public static ILParameterIndex ILParameterIndexFromInstruction (MethodDefinition thisMethod, Instruction operation)
		{
			Code code = operation.OpCode.Code;
			int paramNum;
			return code switch {
				Code.Ldarg
				or Code.Ldarg_0
				or Code.Ldarg_1
				or Code.Ldarg_2
				or Code.Ldarg_3
				or Code.Ldarg_S
				or Code.Ldarga
				or Code.Ldarga_S
				=> GetLdargParamIndex (),

				Code.Starg
				or Code.Starg_S
				=> GetStargParamIndex (),

				_ => throw new ArgumentException ($"{nameof (ILParameterIndex)} expected an ldarg or starg instruction, got {operation.OpCode.Name}")
			};

			ILParameterIndex GetLdargParamIndex ()
			{
				if (code >= Code.Ldarg_0 &&
					code <= Code.Ldarg_3) {
					paramNum = code - Code.Ldarg_0;
				} else {
					var paramDefinition = (ParameterDefinition) operation.Operand;
					if (thisMethod.HasImplicitThis ()) {
						if (paramDefinition == thisMethod.Body.ThisParameter) {
							paramNum = 0;
						} else {
							paramNum = paramDefinition.Index + 1;
						}
					} else {
						paramNum = paramDefinition.Index;
					}
				}
				return (ILParameterIndex) paramNum;
			}
			ILParameterIndex GetStargParamIndex ()
			{
				ParameterDefinition param = (ParameterDefinition) operation.Operand;
				return (ILParameterIndex) param.Sequence;
			}
		}

		public static SourceParameterIndex GetSourceParameter (MethodDefinition method, Instruction operation)
		{
			return SourceParameterIndexFromILParameterIndex (method, ILParameterIndexFromInstruction (method, operation));
		}

		public static SourceParameterIndex SourceParameterIndexFromILParameterIndex (MethodReference method, ILParameterIndex ilIndex)
		{
			if (method.HasImplicitThis ()) {
				if (ilIndex == 0)
					return SourceParameterIndex.This;

				ilIndex--;
			}

			return (SourceParameterIndex) ilIndex;
		}

		public static ILParameterIndex ILParameterIndexFromSourceParameterIndex (MethodReference method, SourceParameterIndex sourceIndex)
		{
			if (method.HasImplicitThis ()) {
				return (ILParameterIndex) sourceIndex + 1;
			}

			return (ILParameterIndex) sourceIndex;
		}
	}
}
