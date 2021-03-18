using Mono.Cecil;

namespace Mono.Linker
{
	public static class ModuleDefinitionExtensions
	{

		public static bool IsCrossgened (this ModuleDefinition module)
		{
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & ModuleAttributes.ILLibrary) != 0;
		}

		public static bool GetMatchingExportedType (this ModuleDefinition module, TypeDefinition typeDefinition, out ExportedType exportedType)
		{
			exportedType = null;
			if (!module.HasExportedTypes || typeDefinition == null)
				return false;

			foreach (var et in module.ExportedTypes)
				if (et.Resolve () == typeDefinition) {
					exportedType = et;
					return true;
				}

			return false;
		}

		public static TypeDefinition ResolveType (this ModuleDefinition module, string typeFullName)
		{
			if (typeFullName == null)
				return null;

			var type = module.GetType (typeFullName);
			if (type != null)
				return type;

			if (!module.HasExportedTypes)
				return null;

			// When resolving a forwarded type from a string, typeFullName should be a simple type name.
			int idx = typeFullName.LastIndexOf ('.');
			(string typeNamespace, string typeName) = idx > 0 ? (typeFullName.Substring (0, idx), typeFullName.Substring (idx + 1)) :
				(string.Empty, typeFullName);

			TypeReference typeReference = new TypeReference (typeNamespace, typeName, module, module);
			return typeReference.Resolve ();
		}
	}
}
