// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class ITypeSymbolExtensions
	{
		[Flags]
		private enum HierarchyFlags
		{
			IsSystemType = 0x01,
			IsSystemReflectionIReflect = 0x02,
		}

		public static bool IsTypeInterestingForDataflow (this ITypeSymbol type)
		{
			if (type.SpecialType == SpecialType.System_String)
				return true;

			return IsSystemType (type) || IsSystemReflectionIReflect (type);
		}

		private static bool IsSystemType (ITypeSymbol type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemType) != 0;
		}

		private static bool IsSystemReflectionIReflect (this ITypeSymbol type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemReflectionIReflect) != 0;
		}


		private static HierarchyFlags GetFlags (ITypeSymbol type)
		{
			HierarchyFlags flags = 0;
			if (type.Name == "IReflect" && type.ContainingNamespace.GetDisplayName () == "System.Reflection") {
				flags |= HierarchyFlags.IsSystemReflectionIReflect;
			}

			ITypeSymbol? baseType = type;
			while (baseType != null) {
				if (baseType.Name == "Type" && baseType.ContainingNamespace.GetDisplayName () == "System") {
					flags |= HierarchyFlags.IsSystemType;
				}

				foreach (var iface in baseType.Interfaces) {
					if (iface.Name == "IReflect" && iface.ContainingNamespace.GetDisplayName () == "System.Reflection") {
						flags |= HierarchyFlags.IsSystemReflectionIReflect;
					}
				}

				baseType = baseType.BaseType;
			}
			return flags;
		}
	}
}
