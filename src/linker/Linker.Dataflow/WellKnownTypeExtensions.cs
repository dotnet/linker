
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace ILLink.Shared.TypeSystemProxy
{
	public static partial class WellKnownTypeExtensions
	{
		public static bool TryGetMetadataType (this WellKnownType wellKnownType, [NotNullWhen (true)] out MetadataType? specialType)
		{
			specialType = wellKnownType switch {
				// TypeReferences of System.Array do not have a MetadataType of MetadataType.Array -- use string checking instead
				WellKnownType.System_Array => null,
				WellKnownType.System_String => MetadataType.String,
				WellKnownType.System_Object => MetadataType.Object,
				_ => null
			};
			return specialType is not null;
		}
	}
}