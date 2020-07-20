using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker
{
	class TypeHierarchyCache
	{
		[Flags]
		private enum BaseTypeFlags
		{
			IsSystemType = 0x01,
		}

		Dictionary<TypeDefinition, BaseTypeFlags> _cache = new Dictionary<TypeDefinition, BaseTypeFlags> ();

		private BaseTypeFlags GetFlags (TypeReference type)
		{
			TypeDefinition resolvedType = type.Resolve ();
			if (resolvedType == null)
				return 0;

			if (_cache.TryGetValue (resolvedType, out var flags)) {
				return flags;
			}

			TypeDefinition baseType = resolvedType;
			while (baseType != null) {
				if (baseType.Name == "Type" && baseType.Namespace == "System") {
					flags |= BaseTypeFlags.IsSystemType;
				}

				baseType = baseType.BaseType?.Resolve ();
			}

			_cache.Add (resolvedType, flags);

			return flags;
		}

		public bool IsSystemType (TypeReference type)
		{
			return (GetFlags (type) & BaseTypeFlags.IsSystemType) != 0;
		}
	}
}
