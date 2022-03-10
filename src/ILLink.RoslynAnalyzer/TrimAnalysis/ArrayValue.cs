// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	partial record ArrayValue
	{
		public ArrayValue (SingleValue size, params MultiValue[] elements) => (Size, _elements) = (size, elements);

		private readonly MultiValue[] _elements;

		public partial bool TryGetValueByIndex (int index, out MultiValue value)
		{
			value = default;
			if (index >= _elements.Length) return false;
			value = _elements[index];
			return true;
		}

		public bool Equals (ArrayValue? other)
		{
			if (other == null) return false;
			if (other is not ArrayValue otherArray) return false;
			if (!otherArray.Size.Equals(Size)) return false;
			for (int i = 0; i < _elements.Length; i++) {
				if (!otherArray.TryGetValueByIndex(i, out var otherElement)
					|| !otherElement.Equals(_elements[i]))
					return false;
			}
			return base.Equals (other);
		}

		public override int GetHashCode ()
		{
			return base.GetHashCode ();
		}
	}
}
