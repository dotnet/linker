﻿using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	class TypeHierarchyCache
	{
		[Flags]
		private enum HierarchyFlags
		{
			IsSystemType = 0x01,
			IsSystemReflectionIReflect = 0x02,
		}

		readonly Dictionary<TypeDefinition, HierarchyFlags> _cache = new Dictionary<TypeDefinition, HierarchyFlags> ();
		readonly LinkContext context;

		public TypeHierarchyCache (LinkContext context)
		{
			this.context = context;
		}

		private HierarchyFlags GetFlags (TypeDefinition resolvedType)
		{
			if (_cache.TryGetValue (resolvedType, out var flags)) {
				return flags;
			}

			if (resolvedType.Name == "IReflect" && resolvedType.Namespace == "System.Reflection") {
				flags |= HierarchyFlags.IsSystemReflectionIReflect;
			}

			TypeDefinition baseType = resolvedType;
			while (baseType != null) {
				if (baseType.Name == "Type" && baseType.Namespace == "System") {
					flags |= HierarchyFlags.IsSystemType;
				}

				if (baseType.HasInterfaces) {
					foreach (var iface in baseType.Interfaces) {
						if (iface.InterfaceType.Name == "IReflect" && iface.InterfaceType.Namespace == "System.Reflection") {
							flags |= HierarchyFlags.IsSystemReflectionIReflect;
						}
					}
				}

				baseType = context.TryResolve (baseType.BaseType);
			}

			if (resolvedType != null)
				_cache.Add (resolvedType, flags);

			return flags;
		}

		public bool IsSystemType (TypeDefinition type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemType) != 0;
		}

		public bool IsSystemReflectionIReflect (TypeDefinition type)
		{
			return (GetFlags (type) & HierarchyFlags.IsSystemReflectionIReflect) != 0;
		}
	}
}
