// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is a known System.Type value. TypeRepresented is the 'value' of the System.Type.
	/// </summary>
	record SystemTypeValue : SingleValue
	{
		public SystemTypeValue (in TypeProxy representedType) => RepresentedType = representedType;

		public readonly TypeProxy RepresentedType;

		public override string ToString () => this.ValueToString (RepresentedType);
	}

	sealed record NullableSystemTypeValue : SystemTypeValue
	{
		public NullableSystemTypeValue (in TypeProxy representedType, in MultiValue underlyingTypeValue) : base (representedType)
			=> UnderlyingTypeValue = underlyingTypeValue;

		public readonly MultiValue UnderlyingTypeValue;
	}
}
