// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// This is the System.RuntimeTypeHandle equivalent to a <see cref="SystemTypeValue"/> node.
	/// </summary>
	sealed record RuntimeTypeHandleValue : SingleValue
	{
		public RuntimeTypeHandleValue (in TypeProxy representedType)
		{
			RepresentedType = representedType;
		}
		public readonly TypeProxy RepresentedType;

		public override string ToString () => this.ValueToString (RepresentedType);
	}

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

	/// <summary>
	/// This represents a type handle of a Nullable<T> where T is an unknown value with DynamicallyAccessedMembers annotations. 
	/// It is necessary to track the underlying type to ensure DynamicallyAccessedMembers annotations on the underlying type match the target parameters where the Nullable is used.
	/// </summary>
	sealed record NullableRuntimeTypeWithDamHandleValue : SingleValue
	{
		public NullableRuntimeTypeWithDamHandleValue (in TypeProxy nullableType, in RuntimeTypeHandleForGenericParameterValue underlyingTypeValue)
		{
			Debug.Assert ((nullableType.Name == "Nullable" || nullableType.Name == "Nullable`1") && nullableType.Namespace == "System");
			NullableType = nullableType;
			UnderlyingTypeValue = underlyingTypeValue;
		}

		public readonly TypeProxy NullableType;
		public readonly RuntimeTypeHandleForGenericParameterValue UnderlyingTypeValue;
	}

}
