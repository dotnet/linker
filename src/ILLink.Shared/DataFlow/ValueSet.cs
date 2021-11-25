using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace ILLink.Shared.DataFlow
{
	public readonly struct ValueSet<TValue> : IEquatable<ValueSet<TValue>>, IEnumerable<TValue>
		where TValue : notnull
	{
		// Since we're going to do lot of type checks for this class a lot, it is much more efficient
		// if the class is sealed (as then the runtime can do a simple method table pointer comparison)
		class SealedHashSet : HashSet<TValue>
		{
			public SealedHashSet () { }
			public SealedHashSet (IEnumerable<TValue> values) : base (values) { }
		}

		public struct Enumerator : IEnumerator<TValue>, IDisposable, IEnumerator
		{
			readonly object? _value;
			int _state;  // 0 before begining, 1 at item, 2 after end
			readonly IEnumerator<TValue>? _enumerator;

			internal Enumerator (object? values)
			{
				_state = 0;
				if (values is SealedHashSet valuesSet) {
					_enumerator = valuesSet.GetEnumerator ();
					_value = default (TValue);
				} else {
					_enumerator = null;
					_value = values;
				}
			}

			// TODO: How to get this to work - without the '!' at the end this complains that default can return null.
			// But how does this work for HashSet<T>? That will return null from Current in reality as well... 
			// It seems that the nullability is not propagated "inside" HashSet (since that one is implemented with TValue being nullable)
			public TValue Current => (_enumerator is not null ? _enumerator.Current : (_state == 1 ? (TValue)_value! : default))!;

			object? IEnumerator.Current => Current;

			public void Dispose ()
			{
			}

			public bool MoveNext ()
			{
				if (_enumerator is not null)
					return _enumerator.MoveNext ();

				if (_value is null)
					return false;

				if (_state > 1)
					return false;

				_state++;
				return _state == 1;
			}

			public void Reset ()
			{
				if (_enumerator is not null)
					_enumerator.Reset ();
				else
					_state = 0;
			}
		}

		// This stores the values. By far the most common case will be either no values, or a single value.
		// Cases where there are multiple values stored are relatively very rare.
		//   null - no values (empty set)
		//   TValue - single value itself
		//   SealedHashSet typed object - multiple values, stored in the hashset
		private readonly object? _values;

		public ValueSet (TValue value) => _values = value;

		public ValueSet (IEnumerable<TValue> values) => _values = new SealedHashSet (values);

		private ValueSet (SealedHashSet values) => _values = values;

		public override bool Equals (object? obj) => obj is ValueSet<TValue> other && Equals (other);

		public bool Equals (ValueSet<TValue> other)
		{
			if (_values == null)
				return other._values == null;
			if (other._values == null)
				return false;

			if (_values is SealedHashSet valuesSet) {
				Debug.Assert (valuesSet.Count > 1);
				if (other._values is SealedHashSet otherValuesSet) {
					Debug.Assert (otherValuesSet.Count > 1);
					return valuesSet.SetEquals (otherValuesSet);
				} else
					return false;
			} else {
				if (other._values is SealedHashSet otherValuesSet) {
					Debug.Assert (otherValuesSet.Count > 1);
					return false;
				}

				return EqualityComparer<TValue>.Default.Equals ((TValue)_values, (TValue)other._values);
			}
		}

		public override int GetHashCode ()
		{
			if (_values == null)
				return typeof (ValueSet<TValue>).GetHashCode ();

			if (_values is SealedHashSet valuesSet) {
				int hashCode = 0;
				foreach (var item in valuesSet)
					hashCode = HashUtils.Combine (hashCode, item);
				return hashCode;
			}

			return _values.GetHashCode ();
		}

		public Enumerator GetEnumerator () => new(_values);
		IEnumerator<TValue> IEnumerable<TValue>.GetEnumerator () => GetEnumerator ();

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public bool Contains (TValue value) => _values is null ? false : _values is SealedHashSet valuesSet ? valuesSet.Contains (value) : EqualityComparer <TValue>.Default.Equals (value, (TValue)_values);

		internal static ValueSet<TValue> Meet (ValueSet<TValue> left, ValueSet<TValue> right)
		{
			if (left._values == null)
				return right;
			if (right._values == null)
				return left;

			if (left._values is not SealedHashSet && right.Contains ((TValue)left._values))
				return right;
				
			if (right._values is not SealedHashSet && left.Contains ((TValue)right._values))
				return left;

			var values = new SealedHashSet (left);
			values.UnionWith (right);
			return new ValueSet<TValue> (values);
		}

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ("{");
			sb.Append (string.Join (",", this.Select (v => v.ToString ())));
			sb.Append ("}");
			return sb.ToString ();
		}
	}
}