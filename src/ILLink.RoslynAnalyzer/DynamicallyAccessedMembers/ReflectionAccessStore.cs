// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public struct ReflectionAccessStore : IEnumerable<ReflectionAccessPattern>
	{
		readonly Dictionary<IOperation, ReflectionAccessPattern> AccessPatterns;

		public ReflectionAccessStore () => AccessPatterns = new Dictionary<IOperation, ReflectionAccessPattern> ();

		public void Add (ReflectionAccessPattern accessPattern)
		{
			// If we already stored a reflection access pattern for this operation,
			// it needs to be updated. The dataflow analysis should result in purely additive
			// changes to the reflection access patterns generated for a given operation,
			// so we can just replace the original access pattern here.

#if DEBUG
			// Validate this in debug mode.
			if (AccessPatterns.TryGetValue (accessPattern.Operation, out var existingAccessPattern)) {
				// The existing pattern source/target should be a subset of the new source/target.
				foreach (SingleValue source in existingAccessPattern.Source)
					Debug.Assert (accessPattern.Source.Contains (source));

				foreach (SingleValue target in existingAccessPattern.Target)
					Debug.Assert (accessPattern.Target.Contains (target));
			}
#endif
			AccessPatterns[accessPattern.Operation] = accessPattern;
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<ReflectionAccessPattern> GetEnumerator () => AccessPatterns.Values.GetEnumerator ();
	}
}