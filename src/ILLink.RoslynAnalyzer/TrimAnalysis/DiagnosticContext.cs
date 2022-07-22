// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct DiagnosticContext
	{
		public List<Diagnostic> Diagnostics { get; } = new ();

		readonly Location? Location { get; init; }

		public DiagnosticContext (Location location)
		{
			Location = location;
		}

		public static DiagnosticContext CreateDisabled () => new () { Location = null };

		public partial void AddDiagnostic (DiagnosticId id, params string[] args)
		{
			if (Location == null)
				return;

			Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, args));
		}

		public partial void AddDiagnostic (DiagnosticId id, ValueWithDynamicallyAccessedMembers sourceAttribute, params string[] args)
		{
			if (Location == null) {
				return;
			}

			switch (sourceAttribute) {
			case FieldValue field:
				Location fieldLocation = field.FieldSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, fieldLocation));
				break;
			case MethodParameterValue mpv:
				Location mpvLocation = mpv.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, mpvLocation));
				break;
			case MethodReturnValue mrv:
				Location mrvLocation = mrv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, mrvLocation));
				break;
			case MethodThisParameterValue mtpv:
				Location mptvLocation = mtpv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, mptvLocation));
				break;
				// case GenericParameterValue gpv:
				// 	Location gpvLocation = gpv.
				// break;
			}
		}
	}
}
