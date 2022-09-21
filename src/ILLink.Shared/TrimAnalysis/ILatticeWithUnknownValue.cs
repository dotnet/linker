// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.Shared.DataFlow;

namespace ILLink.Shared.TrimAnalysis
{
	public interface ILatticeWithUnknownValue<TValue> : ILattice<TValue>
		where TValue : IEquatable<TValue>
	{
		TValue UnknownValue { get; }
	}
}
