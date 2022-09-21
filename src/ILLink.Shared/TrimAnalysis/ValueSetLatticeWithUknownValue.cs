// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	public readonly struct ValueSetLatticeWithUnknownValue<TValue> : ILatticeWithUnknownValue<ValueSet<TValue>>
		where TValue : IEquatable<TValue>
	{
		private readonly TValue _unknownValue;
		public ValueSetLatticeWithUnknownValue (TValue unknownValue)
		{
			_unknownValue = unknownValue;
		}

		public ValueSet<TValue> UnknownValue => new ValueSet<TValue>(_unknownValue);

		public ValueSet<TValue> Top => default;

		public ValueSet<TValue> Meet (ValueSet<TValue> left, ValueSet<TValue> right) => ValueSet<TValue>.Meet (left, right);
	}
}