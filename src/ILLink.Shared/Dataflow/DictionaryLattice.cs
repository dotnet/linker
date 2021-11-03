// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace ILLink.Shared
{
	public class DefaultValueDictionary<TKey, TValue> : Dictionary<TKey, TValue>,
 		IEquatable<DefaultValueDictionary<TKey, TValue>>
		where TKey : IEquatable<TKey>
		where TValue : IEquatable<TValue>
	{
		TValue DefaultValue { get; }

		public DefaultValueDictionary (TValue defaultValue) => DefaultValue = defaultValue;

		public DefaultValueDictionary (DefaultValueDictionary<TKey, TValue> other)
			: base (other)
		{
			DefaultValue = other.DefaultValue;
		}

		public TValue Get (TKey key) => TryGetValue (key, out var value) ? value : DefaultValue;

		public void Set (TKey key, TValue value)
		{
			if (value.Equals (DefaultValue))
				Remove (key);
			else
				this[key] = value;
		}

		// IEquatable
		// Why do I get a nullability warning here but not for SingleValue (if make other non-nullable)?
		public virtual bool Equals (DefaultValueDictionary<TKey, TValue>? other)
		{
			if (other == null)
				throw new InvalidOperationException ();
			if (Count != other.Count)
				return false;

			foreach (var kvp in other) {
				if (!Get (kvp.Key).Equals (kvp.Value))
					return false;
			}

			return true;
		}
	}

	public struct DictionaryLattice<TKey, TValue, TValueLattice> : ILattice<DefaultValueDictionary<TKey, TValue>>
		where TKey : IEquatable<TKey>
		where TValue : IEquatable<TValue>
		where TValueLattice : ILattice<TValue>
	{
		public readonly TValueLattice ValueLattice;

		public DefaultValueDictionary<TKey, TValue> Top { get; }

		public DictionaryLattice (TValueLattice valueLattice)
		{
			ValueLattice = valueLattice;
			Top = new DefaultValueDictionary<TKey, TValue> (valueLattice.Top);
		}
		public DefaultValueDictionary<TKey, TValue> Meet (DefaultValueDictionary<TKey, TValue> left, DefaultValueDictionary<TKey, TValue> right)
		{
			var met = new DefaultValueDictionary<TKey, TValue> (left);
			foreach (var kvp in right) {
				TKey key = kvp.Key;
				TValue rightValue = kvp.Value;
				met.Set (key, ValueLattice.Meet (left.Get (key), rightValue));
			}
			return met;
		}
	}
}