// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	sealed class ReflectionDataFlowAnalysis : LocalDataFlowAnalysis<ReflectionScanner>
	{
		public TrimAnalysisPatternStore TrimAnalysisPatterns { get; }

		public ReflectionDataFlowAnalysis (LinkContext context)
			: base (context)
		{
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (Lattice.LocalsLattice.ValueLattice, context);
		}

		public void AnalyzeMethod (MethodDefinition method, MarkStep parent, MessageOrigin origin)
		{
			if (TryAnalyzeMethod (method, parent, origin)) {
				var reflectionMarker = new ReflectionMarker (Context, parent, enabled: true);
				TrimAnalysisPatterns.MarkAndProduceDiagnostics (reflectionMarker, parent);
			} else {
				var scanner = new ReflectionMethodBodyScanner (Context, parent, origin);
				scanner.InterproceduralScan (method.Body);
			}

		}

		protected override ReflectionScanner GetBodyScanner (
			LinkContext context, MethodDefinition method, MarkStep parent, MessageOrigin origin)
		 => new (context, method, parent, origin, TrimAnalysisPatterns);
	}
}
