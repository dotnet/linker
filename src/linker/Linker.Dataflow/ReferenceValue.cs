// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using ILLink.Shared.DataFlow;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace ILLink.Shared.TrimAnalysis
{
	public abstract record ReferenceValue : SingleValue { }
	public partial record FieldReferenceValue : ReferenceValue
	{
		public readonly FieldDefinition FieldDefinition;
		public FieldReferenceValue (FieldDefinition field)
		{
			FieldDefinition = field;
		}
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
	public partial record LocalVariableReferenceValue : ReferenceValue
	{
		public readonly VariableDefinition LocalDefinition;
		public LocalVariableReferenceValue (VariableDefinition localDef)
		{
			LocalDefinition = localDef;
		}
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
	public partial record ParameterReferenceValue : ReferenceValue
	{
		public readonly MethodDefinition MethodDefinition;
		public readonly int ParameterIndex;
		public ParameterReferenceValue (MethodDefinition methodDefinition, int index)
		{
			MethodDefinition = methodDefinition;
			ParameterIndex = index;
		}
		public override SingleValue DeepCopy ()
		{
			return this;
		}
	}
}
