// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared
{
	// TValue must be a class because the visitor modifies the input value
	// instead of creating new immutable ones from the input.
	// The modification must be visible to the caller, and we can't use ref
	// structs here because OperationVisitor doesn't use ref params.
	public interface ITransfer<TOperation, TValue, TLattice>
		where TValue : class, IEquatable<TValue> // class because the transfer operation is expected to mutate the value in-place
		where TLattice : ILattice<TValue>
	{
		void Transfer (TOperation operation, TValue value);
	}
}