// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Security.Cryptography;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.Shared.TypeSystemProxy
{
	public static partial class WellKnownTypeExtensions
	{
		public static bool TryGetSpecialType (this WellKnownType wellKnownType, out SpecialType? specialType)
		{
			switch (wellKnownType) {
			case WellKnownType.System_String:
				specialType = SpecialType.System_String;
				return true;
			case WellKnownType.System_Nullable_T:
				specialType = SpecialType.System_Nullable_T;
				return true;
			case WellKnownType.System_Array:
				specialType = SpecialType.System_Array;
				return true;
			case WellKnownType.System_Object:
				specialType = SpecialType.System_Object;
				return true;
			default:
				specialType = null;
				return false;
			}
		}
	}
}