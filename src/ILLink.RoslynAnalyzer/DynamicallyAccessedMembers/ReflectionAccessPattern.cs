// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

using MultiValue = ILLink.Shared.HashSetWrapper<ILLink.Shared.SingleValue>;

namespace ILLink.RoslynAnalyzer
{
	public struct ReflectionAccessPattern : IEquatable<ReflectionAccessPattern>
	{
		public readonly MultiValue Source;
		public readonly MultiValue Target;

		// TODO: create abstraction for operation/location suitable for use by
		// roslyn or linker to facilitate sharing
		public readonly IOperation Operation;

		public ReflectionAccessPattern (MultiValue source, MultiValue target, IOperation operation)
		{
			Source = source;
			Target = target;
			Operation = operation;
		}

		public bool Equals (ReflectionAccessPattern other)
		{
			return Source.Equals (other.Source) &&
				Target.Equals (other.Target) &&
				Operation == other.Operation;
		}

		public override int GetHashCode ()
		{
			return HashCode.Combine (Source, Target, Operation);
		}
	}
}