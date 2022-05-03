// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	public readonly struct TrimAnalysisPatternStore
	{
		readonly Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern> AssignmentPatterns;
		readonly Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> MethodCallPatterns;
		readonly LinkContext _context;

		public TrimAnalysisPatternStore (LinkContext context)
		{
			AssignmentPatterns = new Dictionary<(MessageOrigin, bool), TrimAnalysisAssignmentPattern> ();
			MethodCallPatterns = new Dictionary<MessageOrigin, TrimAnalysisMethodCallPattern> ();
			_context = context;
		}

		public void Add (TrimAnalysisAssignmentPattern pattern, bool isReturnValue)
		{
			// In the linker, each pattern should have a unique origin (which has ILOffset)
			// but we don't track the correct ILOffset for return instructions.
			// https://github.com/dotnet/linker/issues/2778
			// For now, work around it with a separate bit.
			AssignmentPatterns.Add ((pattern.Origin, isReturnValue), pattern);
		}

		public void Add (TrimAnalysisMethodCallPattern pattern)
		{
			MethodCallPatterns.Add (pattern.Origin, pattern);
		}

		public void MarkAndProduceDiagnostics (bool diagnosticsEnabled, ReflectionMarker reflectionMarker, MarkStep markStep)
		{
			foreach (var pattern in AssignmentPatterns.Values)
				pattern.MarkAndProduceDiagnostics (diagnosticsEnabled, reflectionMarker, _context);

			foreach (var pattern in MethodCallPatterns.Values)
				pattern.MarkAndProduceDiagnostics (diagnosticsEnabled, _context, reflectionMarker, markStep);
		}
	}
}