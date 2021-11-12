// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public struct TrimAnalysisPattern : IEquatable<TrimAnalysisPattern>
	{
		public readonly MultiValue Source;
		public readonly MultiValue Target;

		// TODO: create abstraction for operation/location suitable for use by
		// roslyn or linker to facilitate sharing
		public readonly IOperation Operation;

		public TrimAnalysisPattern (MultiValue source, MultiValue target, IOperation operation)
		{
			Source = source;
			Target = target;
			Operation = operation;
		}

		public bool Equals (TrimAnalysisPattern other)
		{
			return Source.Equals (other.Source) &&
				Target.Equals (other.Target) &&
				Operation == other.Operation;
		}

		public override int GetHashCode () => HashUtils.Combine (Source, Target, Operation);
	}
}