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
	}
}
