// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
			if (sourceValue is NullableValueWithDynamicallyAccessedMembers nv) {
				sourceValue = nv.UnderlyingTypeValue;
			}
			ISymbol symbol = sourceValue switch {
				FieldValue field => field.FieldSymbol,
				MethodParameterValue mpv => mpv.ParameterSymbol,
				MethodReturnValue mrv => mrv.MethodSymbol,
				MethodThisParameterValue mtpv => mtpv.MethodSymbol,
				GenericParameterValue gpv => gpv.GenericParameter.TypeParameterSymbol,
				_ => throw new InvalidOperationException ()
			};

			if (symbol.DeclaringSyntaxReferences.Length == 0) {
				Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, args));
				return;
			}

			Location symbolLocation = symbol.DeclaringSyntaxReferences[0].GetSyntax ().GetLocation ();
			sourceLocation[0] = symbolLocation;

			Dictionary<string, string?> DAMArgument = new Dictionary<string, string?> ();
			DAMArgument.Add ("attributeArgument", originalValue.DynamicallyAccessedMemberTypes.ToString ());

			Diagnostics.Add (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (id), Location, sourceLocation, DAMArgument.ToImmutableDictionary (), args));
		}
	}
}
