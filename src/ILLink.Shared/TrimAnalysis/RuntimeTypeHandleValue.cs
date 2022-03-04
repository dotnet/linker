// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeValue"/> node.
	/// </summary>
	record RuntimeTypeHandleValue : SingleValue
	{
		public RuntimeTypeHandleValue (in TypeProxy representedType) => RepresentedType = representedType;

		public readonly TypeProxy RepresentedType;

		public override string ToString () => this.ValueToString (RepresentedType);
	}

	sealed record RuntimeNullableTypeHandleValue : RuntimeTypeHandleValue
	{
		public RuntimeNullableTypeHandleValue (in TypeProxy representedType, in MultiValue underlyingTypeValue) : base(representedType)
			=> UnderlyingTypeValue = underlyingTypeValue;

		public readonly MultiValue UnderlyingTypeValue;
	}
}
