// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static partial class Diagnostics
	{
		static readonly Dictionary<Type, List<string>> _supportedDiagnosticsOnType = new Dictionary<Type, List<string>> ();
		static readonly Dictionary<string, DiagnosticDescriptor> _generatedDiagnostics = new Dictionary<string, DiagnosticDescriptor> ();

		static partial void AddGeneratedDiagnostics ();

		static Diagnostics ()
		{
			AddGeneratedDiagnostics ();
		}

		public static DiagnosticDescriptor GetDiagnostic (string diagnosticCode)
		{
			if (!_generatedDiagnostics.TryGetValue (diagnosticCode, out var diagnostic))
				throw new ArgumentException ($"The diagnostic with code {diagnosticCode} was not found.");

			return diagnostic;
		}

		public static DiagnosticDescriptor[] GetSupportedDiagnosticsOnType (Type type)
		{
			var supportedDiagnostics = new List<DiagnosticDescriptor> ();
			foreach (var diagnostic in _supportedDiagnosticsOnType[type]) {
				if (!_generatedDiagnostics.TryGetValue (diagnostic, out var supportedDiagnostic))
					throw new ArgumentException ($"The diagnostic with code '{diagnostic}' was not found in the list of diagnostics supported by the type '{nameof (type)}'.");

				supportedDiagnostics.Add (supportedDiagnostic);
			}

			return supportedDiagnostics.ToArray ();
		}
	}
}
