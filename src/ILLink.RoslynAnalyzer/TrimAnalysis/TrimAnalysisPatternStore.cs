// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore : IEnumerable<ITrimAnalysisPattern>
	{
		readonly Dictionary<(IOperation, bool), ITrimAnalysisPattern> TrimAnalysisPatterns;

		public TrimAnalysisPatternStore ()
		{
			TrimAnalysisPatterns = new Dictionary<(IOperation, bool), ITrimAnalysisPattern> ();
		}

		public void Add (ITrimAnalysisPattern trimAnalysisPattern, bool isReturnValue)
		{
			// Finally blocks will be analyzed multiple times, once for normal control flow and once
			// for exceptional control flow, and these separate analyses could produce different
			// trim analysis patterns.
			// The current algorithm always does the exceptional analysis last, so the final state for
			// an operation will include all analysis patterns (since the exceptional state is a superset)
			// of the normal control-flow state.
			// We still add patterns to the operation, rather than replacing, to make this resilient to
			// changes in the analysis algorithm.
			if (!TrimAnalysisPatterns.TryGetValue ((trimAnalysisPattern.Operation, isReturnValue), out ITrimAnalysisPattern existingPattern)) {
				TrimAnalysisPatterns.Add ((trimAnalysisPattern.Operation, isReturnValue), trimAnalysisPattern);
				return;
			}

			Debug.Assert (trimAnalysisPattern.GetType () == existingPattern.GetType ());
			TrimAnalysisPatterns[(trimAnalysisPattern.Operation, isReturnValue)] = trimAnalysisPattern.Merge (existingPattern);
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<ITrimAnalysisPattern> GetEnumerator () => TrimAnalysisPatterns.Values.GetEnumerator ();
	}
}