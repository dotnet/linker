// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using ILLink.Shared.DataFlow;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.Shared.TrimAnalysis
{
	public partial record ArrayElementReferenceValue (MultiValue ReferencedValue) : ReferenceValue
	{
		public override SingleValue DeepCopy ()
		{
			MultiValue values = new MultiValue (ReferencedValue.Select (v => v.DeepCopy ()));
			return new ArrayElementReferenceValue (values);
		}
	}
}
