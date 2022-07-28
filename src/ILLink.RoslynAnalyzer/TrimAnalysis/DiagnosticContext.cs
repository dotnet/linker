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

		public partial void AddDiagnostic (DiagnosticId id, ValueWithDynamicallyAccessedMembers sourceValue, ValueWithDynamicallyAccessedMembers originalValue, params string[] args)
		{
			if (Location == null) {
				return;
			}

			List<Location> sourceLocation = new ();
			switch (sourceValue) {
			case FieldValue field:
				Location sourceFieldLocation = field.FieldSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(sourceFieldLocation);
				break;
			case MethodParameterValue mpv:
				Location sourceMpvLocation = mpv.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(sourceMpvLocation);
				break;
			case MethodReturnValue mrv:
				Location sourceMrvLocation = mrv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(sourceMrvLocation);
				break;
			case MethodThisParameterValue mtpv:
				Location sourceMptvLocation = mtpv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(sourceMptvLocation);
				break;
			default:
				return;
			}
			switch (originalValue) {
			case FieldValue field:
				Location originalFieldLocation = field.FieldSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(originalFieldLocation);
				break;
			case MethodParameterValue mpv:
				Location originalMpvLocation = mpv.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(originalMpvLocation);
				break;
			case MethodReturnValue mrv:
				Location originalMrvLocation = mrv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation.Add(originalMrvLocation);
				break;
			case MethodThisParameterValue:
				break;
			default:
				return;
			}
			Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, sourceLocation, args));
		}
	}
}
