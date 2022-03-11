﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a Nullable<T> where T is a known SystemTypeValue.
	/// It is necessary to track the underlying type to propagate DynamicallyAccessedMembers annotations to the underlying type when applied to a Nullable.
	/// </summary>
	sealed record NullableSystemTypeValue : SingleValue
	{
		public NullableSystemTypeValue (in TypeProxy nullableType, in TypeProxy underlyingType)
		{
			Debug.Assert (nullableType.IsTypeOf ("System", "Nullable`1"));
			UnderlyingTypeValue = underlyingType;
			NullableType = nullableType;
		}
		public readonly TypeProxy NullableType;

		public readonly TypeProxy UnderlyingTypeValue;

		public override SingleValue DeepCopy () => this; // This value is immutable

		public override string ToString () => this.ValueToString (UnderlyingTypeValue, NullableType);
	}
}
