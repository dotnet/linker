using System.Diagnostics;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	internal partial class ValueHolder : IValueHolder
    {
		public MultiValue Value { get; private set; }
		internal ValueHolderKind Kind { get; init; }
		internal ValueHolder (MultiValue value, ValueHolderKind kind)
		{
			Value = value;
			Kind = kind;
		}
		internal partial class ValueHolderReference<TKey> : IValueHolder
		{
			readonly ValueHolder Holder;
			public readonly TKey Key;
			internal ValueHolderReference (ValueHolder slot, TKey key)
			{
				Holder = slot;
				Key = key;
			}
			public  MultiValue Value => Holder.Value;
			internal ValueHolderKind Kind => Holder.Kind;
			internal void SetValue (MultiValue value)
			{
				Debug.Assert (IsMutable);
				Holder.Value = value;
			}
			public bool IsMutable { get => IsMutableKind (Kind); }
		}


		public static bool IsMutableKind (ValueHolderKind kind)
		{
			return kind switch {
				ValueHolderKind.LocalVariable => true,
				ValueHolderKind.Field => true,
				ValueHolderKind.Parameter => false,
				ValueHolderKind.ArrayElement => true,
				ValueHolderKind.Stack => false
			};
		}
    }

	internal enum ValueHolderKind
	{
		LocalVariable,
		Field,
		Parameter,
		ArrayElement,
		Stack
	}

	internal interface IValueHolder
	{
		MultiValue Value { get; }
	}
}
