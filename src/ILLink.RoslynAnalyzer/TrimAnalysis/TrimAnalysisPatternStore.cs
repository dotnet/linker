// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly struct TrimAnalysisPatternStore : IEnumerable<TrimAnalysisPattern>
	{
		readonly Dictionary<IOperation, TrimAnalysisPattern> TrimAnalysisPatterns;

		public TrimAnalysisPatternStore () => TrimAnalysisPatterns = new Dictionary<IOperation, TrimAnalysisPattern> ();

		public void Add (TrimAnalysisPattern trimAnalysisPattern)
		{
			// If we already stored a trim analysis pattern for this operation,
			// it needs to be updated. The dataflow analysis should result in purely additive
			// changes to the trim analysis patterns generated for a given operation,
			// so we can just replace the original analysis pattern here.

			// This is no longer true since we effectively clone the finally blocks,
			// but continue using the same operation as the warning origin.

			// TODO: add instead of replace, to be safe? Or add extra handling for finally blocks.
			TrimAnalysisPatterns[trimAnalysisPattern.Operation] = trimAnalysisPattern;
		}

		IEnumerator IEnumerable.GetEnumerator () => GetEnumerator ();

		public IEnumerator<TrimAnalysisPattern> GetEnumerator () => TrimAnalysisPatterns.Values.GetEnumerator ();
	}
}