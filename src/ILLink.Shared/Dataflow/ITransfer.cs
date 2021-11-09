// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	// TValue is a class because the typical implementation is a
	// visitor which modifies the input value instead of creating new immutable ones from the input.
	public interface ITransfer<TOperation, TValue, TLattice>
		where TValue : class, IEquatable<TValue>
		where TLattice : ILattice<TValue>
	{
		void Transfer (TOperation operation, TValue value);
	}
}