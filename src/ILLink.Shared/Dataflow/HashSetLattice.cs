using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace ILLink.Shared
{
	public struct HashSetWrapper<TValue> : IEquatable<HashSetWrapper<TValue>>, IEnumerable<TValue>
		where TValue : notnull
	{
		// TODO: use immutable collection?
		public readonly HashSet<TValue>? Values;

		public HashSetWrapper (HashSet<TValue> values) => Values = values;

		public HashSetWrapper (TValue value) => Values = new HashSet<TValue> () { value };

		public override bool Equals (object? obj) => obj is HashSetWrapper<TValue> other && Equals (other);

		public bool Equals (HashSetWrapper<TValue> other)
		{
			if (Values == null)
				return other.Values == null;
			if (other.Values == null)
				return false;

			return Values.SetEquals (other.Values); // TODO: performance? compare ValueNodeHashSet
		}

		public override int GetHashCode ()
		{
			if (Values == null)
				return typeof (HashSetWrapper<TValue>).GetHashCode ();
			return HashUtils.CalcHashCodeEnumerable (Values);
		}

		public IEnumerator<TValue> GetEnumerator ()
		{
			if (Values == null)
				yield break;

			foreach (var value in Values)
				yield return value;
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		// TODO: implement ICollection or IReadOnlySet?
		// TODO: what should Contains(Top) return?
		public bool Contains (TValue value) => Values?.Contains (value) ?? false;

		public override string ToString ()
		{
			StringBuilder sb = new ();
			sb.Append ("{");
			if (Values != null) {
				bool first = true;
				foreach (var v in Values) {
					if (!first)
						sb.Append (",");
					first = false;
					sb.Append (v.ToString ());
				}
			}
			sb.Append ("}");
			return sb.ToString ();
		}
	}

	public struct HashSetLattice<TValue> : ILattice<HashSetWrapper<TValue>>
		where TValue : IEquatable<TValue>
	{
		public HashSetWrapper<TValue> Top => default;

		public HashSetWrapper<TValue> Meet (HashSetWrapper<TValue> left, HashSetWrapper<TValue> right)
		{
			if (left.Values == null)
				return right; // TODO: OK only if it's immutable
			if (right.Values == null)
				return left;

			var values = new HashSet<TValue> (left.Values);
			values.UnionWith (right.Values);
			return new HashSetWrapper<TValue> (values);
		}
	}
}