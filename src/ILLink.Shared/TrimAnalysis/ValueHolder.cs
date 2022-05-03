using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	/// <summary>
	/// Represents a single location in memory
	/// </summary>
	public partial class ValueHolder
    {
		public MultiValue Value { get; private set; }
		internal ValueHolder (MultiValue value)
		{
			Value = value;
		}
		/// <summary>
		/// Represents a reference to a location that can hold a value. Exposes the ability to mutate the value held at a location.
		/// </summary>
		public partial record Reference : SingleValue
		{
			internal Reference (ValueHolder holder, ValueHolderKind kind)
			{
				Holder = holder;
				Kind = kind;
			}

			private Reference (SingleValue value, ValueHolderKind kind)
			{
				_value = value;
				Kind = kind;
				Holder = null;
			}
			public static Reference FieldReference (MultiValue value)
			{
				return new Reference (value, ValueHolderKind.Field);
			}

			public static Reference ParameterReference (MultiValue value)
			{
				return new Reference (value, ValueHolderKind.Parameter);
			}

			public readonly ValueHolderKind Kind;

			readonly ValueHolder? Holder;

			readonly SingleValue? _value;

			public MultiValue Value { get => _value ?? Holder!.Value; }

			internal void SetValue (MultiValue value)
			{
				if (!IsMutable)
					throw new InvalidOperationException ($"Cannot assign new value to value location type {Enum.GetName(typeof(ValueHolderKind), Kind)}");
				Holder!.Value = value;
			}

			internal void SetToMeetValue (MultiValue value)
			{
				if (!IsMutable)
					throw new InvalidOperationException ($"Cannot assign new value to value location type {Enum.GetName(typeof(ValueHolderKind), Kind)}");
				Holder!.Value = MultiValue.Meet (Holder.Value, value);
			}
			public bool IsMutable { get => IsMutableKind (Kind); }

			public override SingleValue DeepCopy ()
			{
				if (Holder is not null)
					return new Reference (Holder, Kind);
				return new Reference (_value!, Kind);
			}
		}

		public static bool IsMutableKind (ValueHolderKind kind)
		{
			return kind switch {
				ValueHolderKind.LocalVariable => true,
				ValueHolderKind.Field => false,
				ValueHolderKind.Parameter => false,
				ValueHolderKind.ArrayElement => true,
				_ => false
			};
		}
    }

	/// <summary>
	/// Represents a reference type with fields, indices, or other members that hold values
	/// </summary>
	/// <typeparam name="TKey">The type that is used to find the values of fields / indices stored in the object. e.g. int or ConstIntValue for arrays or indexable types</typeparam>
	/// <typeparam name="TValue">The type of the values stored in the reference type</typeparam>
	/// <remarks>This is just a prototype for a potential value we could use</remarks>
	internal record ReferenceTypeValueHolder<TKey, TValue> : SingleValue
		where TKey : notnull
	{
		public ReferenceTypeValueHolder (Dictionary<TKey, TValue> values)
		{
			_values = values;
		}
		private Dictionary<TKey, TValue> _values;

		public virtual bool TryGetValue (TKey key, out TValue value) => _values.TryGetValue (key, out value);

		public virtual void SetValue (TKey key, TValue value) => _values.Add (key, value);

		public virtual void SetToReferenceOf(ReferenceTypeValueHolder<TKey, TValue> other)
		{
			_values = other._values;
		}

		public virtual void CopyFrom (ReferenceTypeValueHolder<TKey, TValue> other)
		{
			_values.Clear ();
			foreach (TKey key in other._values.Keys) {
				TValue value = other._values[key];
				_values[key] = value;
			}
		}

		public override SingleValue DeepCopy ()
		{
			var newVals = new Dictionary<TKey, TValue> ();
			foreach (TKey key in _values.Keys) {
				TValue value = _values[key];
				newVals.Add (key, value);
			}
			return new ReferenceTypeValueHolder<TKey, TValue> (newVals);
		}
	}

	public enum ValueHolderKind
	{
		LocalVariable,
		Field,
		Parameter,
		ArrayElement
	}
}
