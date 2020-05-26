using System;
using System.Collections.Generic;
using System.Text;
using Mono.Cecil;

namespace Mono.Linker
{
	public static class TypeDefinitionExtensions
	{
		public static bool HasInterface (this TypeDefinition type, TypeDefinition interfaceType, out InterfaceImplementation implementation)
		{
			implementation = null;
			if (!type.HasInterfaces)
				return false;

			foreach (var iface in type.Interfaces) {
				if (iface.InterfaceType.Resolve () == interfaceType) {
					implementation = iface;
					return true;
				}
			}

			return false;
		}

		public static TypeReference GetEnumUnderlyingType (this TypeDefinition enumType)
		{
			foreach (var field in enumType.Fields) {
				if (!field.IsStatic) {
					return field.FieldType;
				}
			}

			throw new MissingFieldException ($"Enum type '{enumType.FullName}' is missing instance field");
		}

		public static bool IsMulticastDelegate (this TypeDefinition td)
		{
			return td.BaseType?.Name == "MulticastDelegate" && td.BaseType.Namespace == "System";
		}

		public static bool IsSerializable (this TypeDefinition td)
		{
			return (td.Attributes & TypeAttributes.Serializable) != 0;
		}

		// Takes a member signature (not including the declaring type) and returns the matching members on the type.
		public static IEnumerable<IMemberDefinition> FindMembersByDocumentationSignature (this TypeDefinition type, string signature)
		{
			int index = 0;
			var results = new List<IMemberDefinition> ();
			var nameBuilder = new StringBuilder ();
			var (name, arity) = DocumentationSignatureParser.ParseTypeOrNamespaceName (signature, ref index, nameBuilder);
			DocumentationSignatureParser.GetMatchingMembers (signature, ref index, type.Module, type, name, arity, DocumentationSignatureParser.MemberType.All, results);
			return results;
		}
	}
}