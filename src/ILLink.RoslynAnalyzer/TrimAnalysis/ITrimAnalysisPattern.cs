// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public interface ITrimAnalysisPattern
	{
		public IOperation Operation { get; }

		public IEnumerable<Diagnostic> ReportDiagnostics ();

		public ITrimAnalysisPattern Merge (ITrimAnalysisPattern pattern);
	}
}
