﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly DiagnosticContext _diagnosticContext;
		readonly DiagnosticSolver _diagnosticSolver;
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods
#pragma warning disable IDE0060 // Unused parameters - should be removed once methods are actually implemented

		public RequireDynamicallyAccessedMembersAction (
			DiagnosticContext diagnosticContext,
			DiagnosticSolver diagnosticSolver)
		{
			_diagnosticContext = diagnosticContext;
			_diagnosticSolver = diagnosticSolver;
		}

		private partial bool TryResolveTypeNameAndMark (string typeName, out TypeProxy type)
		{
			// TODO: Implement type name resolution to type symbol
			type = default;
			return false;
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes) =>
			_diagnosticSolver.SolveDiagnosticsForDynamicallyAccessedMembers (_diagnosticContext, type.Type, dynamicallyAccessedMemberTypes);
	}
}
