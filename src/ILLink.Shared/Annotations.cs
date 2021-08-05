// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ILLink.Shared
{
    internal static class Annotations
    {
		public static bool SourceHasRequiredAnnotations (
			DynamicallyAccessedMemberTypes? sourceMemberTypes,
			DynamicallyAccessedMemberTypes? targetMemberTypes,
			out string missingMemberTypesString)
		{
			missingMemberTypesString = string.Empty;
			if (targetMemberTypes == null)
				return true;

			sourceMemberTypes ??= DynamicallyAccessedMemberTypes.None;
			var missingMemberTypesList = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
				.Cast<DynamicallyAccessedMemberTypes> ()
				.Where (damt => (damt & targetMemberTypes & ~sourceMemberTypes) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (missingMemberTypesList.Count == 0)
				return true;

			if (missingMemberTypesList.Contains (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				missingMemberTypesList.SingleOrDefault (mt => mt == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor) is var ppc &&
				ppc != DynamicallyAccessedMemberTypes.None)
				missingMemberTypesList.Remove (ppc);

			missingMemberTypesString = string.Join (", ", missingMemberTypesList.Select (mmt => $"'{nameof (DynamicallyAccessedMemberTypes)}.{mmt}'"));
			return false;
		}
	}
}
