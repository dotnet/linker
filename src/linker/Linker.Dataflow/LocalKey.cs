// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil.Cil;

namespace ILLink.Shared.DataFlow
{
	public readonly struct LocalKey : IEquatable<LocalKey>
	{
		public readonly VariableDefinition Local;

		public LocalKey (VariableDefinition local) => Local = local;

		public bool Equals (LocalKey other) => Local.Equals (other.Local);

		public override int GetHashCode () => Local.GetHashCode ();
	}
}