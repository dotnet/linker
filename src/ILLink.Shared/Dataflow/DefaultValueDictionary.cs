// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ILLink.Shared
{
	public struct DefaultValueDictionary<TKey, TValue> : IEquatable<DefaultValueDictionary<TKey, TValue>>,
		IEnumerable<KeyValuePair<TKey, TValue>>
		where TKey : IEquatable<TKey>
		where TValue : IEquatable<TValue>
	{
		Dictionary<TKey, TValue>? Dictionary;

		readonly TValue DefaultValue;

		public DefaultValueDictionary (TValue defaultValue) => (Dictionary, DefaultValue) = (null, defaultValue);

		public DefaultValueDictionary (DefaultValueDictionary<TKey, TValue> other) => (Dictionary, DefaultValue) = (other.Dictionary, other.DefaultValue);

		public TValue Get (TKey key) => Dictionary?.TryGetValue (key, out var value) == true ? value : DefaultValue;

		public void Set (TKey key, TValue value)
		{
			if (value.Equals (DefaultValue))
				Dictionary?.Remove (key);
			else
				(Dictionary ??= new Dictionary<TKey, TValue> ())[key] = value;
		}

		public bool Equals (DefaultValueDictionary<TKey, TValue> other)
		{
			if (!DefaultValue.Equals (other.DefaultValue))
				return false;

			if (Dictionary == null)
				return other.Dictionary == null;

			if (other.Dictionary == null)
				return false;

			if (Dictionary.Count != other.Dictionary.Count)
				return false;

			foreach (var kvp in other.Dictionary) {
				if (!Get (kvp.Key).Equals (kvp.Value))
					return false;
			}

			return true;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator ()
		{
			return Dictionary?.GetEnumerator () ?? Enumerable.Empty<KeyValuePair<TKey, TValue>> ().GetEnumerator ();
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();
	}
}