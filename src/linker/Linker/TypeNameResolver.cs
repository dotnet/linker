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

		static bool TryParseTypeName (string typeNameString, out TypeName typeName)
		{
			typeName = null;
			try {
				typeName = TypeParser.ParseTypeName (typeNameString);
				return typeName != null;
			} catch (ArgumentException) {
				return false;
			} catch (System.IO.FileLoadException) {
				return false;
			}
		}

		public TypeReference ResolveTypeName (string typeNameString, out AssemblyDefinition foundAssembly)
		{
			foundAssembly = null;
			if (string.IsNullOrEmpty (typeNameString))
				return null;

			if (!TryParseTypeName (typeNameString, out TypeName parsedTypeName))
				return null;

			if (parsedTypeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName)
				return ResolveTypeName (null, assemblyQualifiedTypeName, out foundAssembly);

			var nonQualifiedTypeName = parsedTypeName as NonQualifiedTypeName;
			foreach (var assemblyDefinition in _context.GetAssemblies ()) {
				var foundType = ResolveTypeNameInAssembly (assemblyDefinition, nonQualifiedTypeName);
				if (foundType != null) {
					foundAssembly = assemblyDefinition;
					return foundType;
				}
			}

			return null;
		}

		public TypeReference ResolveTypeNameInAssembly (AssemblyDefinition assembly, string typeNameString)
		{
			if (!TryParseTypeName (typeNameString, out TypeName typeName) || typeName is AssemblyQualifiedTypeName)
				return null;
			return ResolveTypeNameInAssembly (assembly, typeName as NonQualifiedTypeName);
		}

		public TypeReference ResolveTypeNameInAssembly (AssemblyDefinition assembly, NonQualifiedTypeName typeName)
		{
			if (assembly == null || typeName == null)
				return null;

			if (typeName is ConstructedGenericTypeName constructedGenericTypeName) {
				var genericTypeRef = ResolveTypeNameInAssembly (assembly, constructedGenericTypeName.GenericType);
				if (genericTypeRef == null)
					return null;

				TypeDefinition genericType = genericTypeRef.Resolve ();
				var genericInstanceType = new GenericInstanceType (genericType);
				foreach (var arg in constructedGenericTypeName.GenericArguments) {
					var genericArgument = ResolveTypeName (assembly, arg, out _);
					if (genericArgument == null)
						return null;

					genericInstanceType.GenericArguments.Add (genericArgument);
				}

				return genericInstanceType;
			} else if (typeName is HasElementTypeName elementTypeName) {
				var elementType = ResolveTypeName (assembly, elementTypeName.ElementTypeName, out _);
				if (elementType == null)
					return null;

				return typeName switch
				{
					ArrayTypeName _ => new ArrayType (elementType),
					MultiDimArrayTypeName multiDimArrayTypeName => new ArrayType (elementType, multiDimArrayTypeName.Rank),
					ByRefTypeName _ => new ByReferenceType (elementType),
					PointerTypeName _ => new PointerType (elementType),
					_ => elementType
				};
			}

			return assembly.MainModule.GetType (typeName.ToString ());
		}

		TypeReference ResolveTypeName (AssemblyDefinition assembly, TypeName typeName, out AssemblyDefinition foundAssembly)
		{
			if (typeName is AssemblyQualifiedTypeName assemblyQualifiedTypeName) {
				// In this case we ignore the assembly parameter since the type name has assembly in it
				// Resolving a type name should never throw.
				foundAssembly = _context.TryResolve (assemblyQualifiedTypeName.AssemblyName.Name);
				if (foundAssembly == null)
					return null;
				return ResolveTypeNameInAssembly (foundAssembly, assemblyQualifiedTypeName.TypeName);
			}

			foundAssembly = assembly;
			return ResolveTypeNameInAssembly (assembly, typeName as NonQualifiedTypeName);
		}
	}
}