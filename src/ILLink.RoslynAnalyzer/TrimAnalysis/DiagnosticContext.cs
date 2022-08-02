// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
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

			Location[] sourceLocation = new Location[1];
			switch (sourceValue) {
			case FieldValue field:
				Location sourceFieldLocation = field.FieldSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation[0] = sourceFieldLocation;
				break;
			case MethodParameterValue mpv:
				Location sourceMpvLocation = mpv.ParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation[0] = sourceMpvLocation;
				break;
			case MethodReturnValue mrv:
				Location sourceMrvLocation = mrv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation[0] = sourceMrvLocation;
				break;
			case MethodThisParameterValue mtpv:
				Location sourceMptvLocation = mtpv.MethodSymbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
				sourceLocation[0] = sourceMptvLocation;
				break;
			case GenericParameterValue gpv:
				Location sourceGpvLocation = gpv.GenericParameter.TypeParameterSymbol.DeclaringSyntaxReferences[0].GetSyntax().GetLocation();
				sourceLocation[0] = sourceGpvLocation;
				break;
			default:
				return;
			}

			Dictionary<string, string?> DAMArgument = new Dictionary<string, string?> ();
			DAMArgument.Add ("attributeArgument", originalValue.DynamicallyAccessedMemberTypes.ToString ());

			Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, sourceLocation, DAMArgument.ToImmutableDictionary(), args));
		}
	}
}
