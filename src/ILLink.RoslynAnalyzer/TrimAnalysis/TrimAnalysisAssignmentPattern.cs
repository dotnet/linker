// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TrimAnalysis;
using Microsoft.CodeAnalysis;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace ILLink.RoslynAnalyzer.TrimAnalysis
{
	public readonly record struct TrimAnalysisAssignmentPattern (
		MultiValue Source,
		MultiValue Target,
		IOperation Operation)
	{
		public TrimAnalysisAssignmentPattern Merge (ValueSetLattice<SingleValue> lattice, TrimAnalysisAssignmentPattern other)
		{
			Debug.Assert (Operation == other.Operation);

			return new TrimAnalysisAssignmentPattern (
				lattice.Meet (Source, other.Source),
				lattice.Meet (Target, other.Target),
				Operation);
		}

		public IEnumerable<Diagnostic> ReportDiagnostics ()
		{
			foreach (var sourceValue in Source) {
				foreach (var targetValue in Target) {
					// The target should always be an annotated value, but the visitor design currently prevents
					// declaring this in the type system.
					if (targetValue is not ValueWithDynamicallyAccessedMembers targetWithDynamicallyAccessedMembers)
						throw new NotImplementedException ();

					var requireDynamicallyAccessedMembersAction = new RequireDynamicallyAccessedMembersAction ();
					var diagnosticContext = new DiagnosticContext (Operation.Syntax.GetLocation ());
					requireDynamicallyAccessedMembersAction.Invoke (diagnosticContext, sourceValue, targetWithDynamicallyAccessedMembers);

					foreach (var diagnostic in diagnosticContext.Diagnostics)
						yield return diagnostic;
				}
			}
		}
	}
}