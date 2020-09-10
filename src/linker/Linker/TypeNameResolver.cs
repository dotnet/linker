using System;
using System.Reflection.Runtime.TypeParsing;
using Mono.Cecil;

namespace Mono.Linker
{
	internal class TypeNameResolver
	{
		readonly LinkContext _context;

		public TypeNameResolver (LinkContext context)
		{
			_context = context;
		}

		public TypeReference ResolveTypeName (string typeNameString)
		{
			if (string.IsNullOrEmpty (typeNameString))
				return null;

			TypeName parsedTypeName;
			try {
				parsedTypeName = TypeParser.ParseTypeName (typeNameString);
			} catch (ArgumentException) {
				return null;
			} catch (System.IO.FileLoadException) {
				return null;
			}

			if (parsedTypeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				AssemblyDefinition assembly = _context.GetLoadedAssembly (assemblyQualifiedTypeName.AssemblyName.Name);
				return ResolveTypeName (assembly, assemblyQualifiedTypeName.TypeName);
			}

			foreach (var assemblyDefiniton in _context.GetAssemblies ()) {
				var foundType = ResolveTypeName (assemblyDefiniton, parsedTypeName);
				if (foundType != null)
					return foundType;
			}

			return null;
		}

		public static TypeReference ResolveTypeName (AssemblyDefinition assembly, string typeNameString)
		{
			return ResolveTypeName (assembly, TypeParser.ParseTypeName (typeNameString));
		}

		static TypeReference ResolveTypeName (AssemblyDefinition assembly, TypeName typeName)
		{
			if (assembly == null)
				return null;

			if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				return ResolveTypeName (assembly, assemblyQualifiedTypeName.TypeName);
			} else if (typeName is ConstructedGenericTypeName constructedGenericTypeName) {
				var genericTypeDef = ResolveTypeName (assembly, constructedGenericTypeName.GenericType)?.Resolve ();
				var genericInstanceType = new GenericInstanceType (genericTypeDef);
				foreach (var arg in constructedGenericTypeName.GenericArguments) {
					genericInstanceType.GenericArguments.Add (ResolveTypeName (assembly, arg));
				}

				return genericInstanceType;
			} else if (typeName is HasElementTypeName elementTypeName) {
				return ResolveTypeName (assembly, elementTypeName.ElementTypeName);
			}

			return assembly.MainModule.GetType (typeName.ToString ());
		}
	}
}