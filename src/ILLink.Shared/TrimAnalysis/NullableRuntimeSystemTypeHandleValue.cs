// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This represents a type handle Nullable<T> where T is a known SystemTypeValue.
	/// It is necessary to track the underlying type to propagate DynamicallyAccessedMembers annotations to the underlying type when applied to a Nullable.
	/// </summary>
	sealed record NullableRuntimeSystemTypeHandleValue : SingleValue
	{
		public NullableRuntimeSystemTypeHandleValue (in TypeProxy nullableType, in TypeProxy underlyingType)
		{
			Debug.Assert ((nullableType.Name == "Nullable" || nullableType.Name == "Nullable`1") && nullableType.Namespace == "System");
			UnderlyingTypeValue = underlyingType;
			NullableType = nullableType;
		}
		public readonly TypeProxy NullableType;

		public readonly TypeProxy UnderlyingTypeValue;
	}
}
