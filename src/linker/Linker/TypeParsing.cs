using System;
using System.Reflection.Runtime.TypeParsing;
using System.Reflection.Runtime.Assemblies;
using Mono.Cecil;

namespace Mono.Linker
{
	internal class ReflectionTypeNameResolver
	{
		private readonly LinkContext _context;

		public ReflectionTypeNameResolver (LinkContext context)
		{
			_context = context;
		}

		public TypeDefinition ResolveTypeName (string typeNameString)
		{
			if (string.IsNullOrEmpty (typeNameString))
				return null;

			AssemblyQualifiedTypeName parsedTypeName;
			try {
				parsedTypeName = TypeParser.ParseAssemblyQualifiedTypeName (typeNameString);
			} catch (ArgumentException) {
				return null;
			} catch (System.IO.FileLoadException) {
				return null;
			}

			return ResolveTypeName (parsedTypeName);
		}

		private TypeDefinition ResolveTypeName (TypeName typeName)
		{
			if (typeName is AssemblyQualifiedTypeName) {
				AssemblyQualifiedTypeName assemblyQualifiedTypeName = (AssemblyQualifiedTypeName) typeName;
				RuntimeAssemblyName assemblyName = assemblyQualifiedTypeName.AssemblyName;
				foreach (var assemblyDefinition in _context.GetAssemblies ()) {
					if (assemblyName != null && assemblyDefinition.Name.Name != assemblyName.Name)
						continue;

					var foundType = ResolveTypeName (assemblyDefinition, assemblyQualifiedTypeName.TypeName);
					if (foundType == null)
						continue;

					return foundType;
				}

				return null;
			} else if (typeName is NonQualifiedTypeName) {
				return ResolveTypeName (null, (NonQualifiedTypeName) typeName);
			}

			// This is unreachable
			throw new NotImplementedException ();
		}

		public TypeDefinition ResolveTypeName (AssemblyDefinition assembly, NonQualifiedTypeName typeName)
		{
			if (typeName is ConstructedGenericTypeName) {
				ConstructedGenericTypeName genericTypeName = (ConstructedGenericTypeName) typeName;
				return assembly.MainModule.GetType (genericTypeName.GenericType.ToString ());
			} else if (typeName is HasElementTypeName) {
				HasElementTypeName elementTypeName = (HasElementTypeName) typeName;
				TypeDefinition elementType = ResolveTypeName (assembly, elementTypeName.ElementTypeName as NonQualifiedTypeName);
				if (elementType == null)
					return null;

				return assembly.MainModule.GetType (elementType.ToString ());
			}

			return assembly.MainModule.GetType (typeName.ToString ());
		}

	}
}