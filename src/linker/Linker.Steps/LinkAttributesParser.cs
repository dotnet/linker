// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LinkAttributesParser : ProcessLinkerXmlBase
	{
		AttributeInfo _attributeInfo;

		public LinkAttributesParser (LinkContext context, Stream documentStream, string xmlDocumentLocation)
			: base (context, documentStream, xmlDocumentLocation)
		{
		}

		public LinkAttributesParser (LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Parse (AttributeInfo xmlInfo)
		{
			_attributeInfo = xmlInfo;
			bool stripLinkAttributes = _context.IsOptimizationEnabled (CodeOptimizations.RemoveLinkAttributes, _resourceAssembly);
			ProcessXml (stripLinkAttributes, _context.IgnoreLinkAttributes);
		}

		CustomAttribute[] ProcessAttributes (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			XPathNodeIterator iterator = nav.SelectChildren ("attribute", string.Empty);
			var builder = new ArrayBuilder<CustomAttribute> ();
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
					continue;

				TypeDefinition attributeType;
				string internalAttribute = GetAttribute (iterator.Current, "internal");
				if (!string.IsNullOrEmpty (internalAttribute)) {
					attributeType = GenerateRemoveAttributeInstancesAttribute ();
					if (attributeType == null)
						continue;

					// TODO: Replace with IsAttributeType check once we have it
					if (provider is not TypeDefinition) {
						LogWarning ($"Internal attribute '{attributeType.Name}' can only be used on attribute types", 2048, iterator.Current);
						continue;
					}
				} else {
					string attributeFullName = GetFullName (iterator.Current);
					if (string.IsNullOrEmpty (attributeFullName)) {
						LogWarning ($"'attribute' element does not contain attribute 'fullname' or it's empty", 2029, iterator.Current);
						continue;
					}

					if (!GetAttributeType (iterator, attributeFullName, out attributeType))
						continue;
				}

				CustomAttribute customAttribute = CreateCustomAttribute (iterator, attributeType);
				if (customAttribute != null) {
					_context.LogMessage ($"Assigning external custom attribute '{FormatCustomAttribute (customAttribute)}' instance to '{provider}'");
					builder.Add (customAttribute);
				}
			}

			return builder.ToArray ();

			static string FormatCustomAttribute (CustomAttribute ca)
			{
				StringBuilder sb = new StringBuilder ();
				sb.Append (ca.Constructor.GetDisplayName ());
				sb.Append (" { args: ");
				for (int i = 0; i < ca.ConstructorArguments.Count; ++i) {
					if (i > 0)
						sb.Append (", ");

					var caa = ca.ConstructorArguments[i];
					sb.Append ($"{caa.Type.GetDisplayName ()} {caa.Value}");
				}
				sb.Append (" }");

				return sb.ToString ();
			}
		}

		TypeDefinition GenerateRemoveAttributeInstancesAttribute ()
		{
			if (_context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition != null)
				return _context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition;

			var voidType = BCL.FindPredefinedType ("System", "Void", _context);
			if (voidType == null)
				return null;

			var attributeType = BCL.FindPredefinedType ("System", "Attribute", _context);
			if (attributeType == null)
				return null;

			var objectType = BCL.FindPredefinedType ("System", "Object", _context);
			if (objectType == null)
				return null;

			//
			// Generates metadata information for internal type
			//
			// public sealed class RemoveAttributeInstancesAttribute : Attribute
			// {
			//		public RemoveAttributeInstancesAttribute () {}
			//		public RemoveAttributeInstancesAttribute (object value1) {}
			// }
			//
			var td = new TypeDefinition ("", "RemoveAttributeInstancesAttribute", TypeAttributes.Public);
			td.BaseType = attributeType;

			const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Final;
			var ctor = new MethodDefinition (".ctor", ctorAttributes, voidType);
			td.Methods.Add (ctor);

			ctor = new MethodDefinition (".ctor", ctorAttributes, voidType);
			ctor.Parameters.Add (new ParameterDefinition (objectType));
			td.Methods.Add (ctor);

			return _context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition = td;
		}

		CustomAttribute CreateCustomAttribute (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			CustomAttributeArgument[] arguments = ReadCustomAttributeArguments (iterator, attributeType);

			MethodDefinition constructor = FindBestMatchingConstructor (attributeType, arguments);
			if (constructor == null) {
				LogWarning (
					$"Could not find matching constructor for custom attribute '{attributeType.GetDisplayName ()}' arguments",
					2022,
					iterator.Current);
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			foreach (var argument in arguments)
				customAttribute.ConstructorArguments.Add (argument);

			ReadCustomAttributeProperties (iterator.Current.SelectChildren ("property", string.Empty), attributeType, customAttribute);

			return customAttribute;
		}

		static MethodDefinition FindBestMatchingConstructor (TypeDefinition attributeType, CustomAttributeArgument[] args)
		{
			var methods = attributeType.Methods;
			for (int i = 0; i < attributeType.Methods.Count; ++i) {
				var m = methods[i];
				if (!m.IsInstanceConstructor ())
					continue;

				var p = m.Parameters;
				if (args.Length != p.Count)
					continue;

				bool match = true;
				for (int ii = 0; match && ii != args.Length; ++ii) {
					//
					// No candidates betterness, only exact matches are supported
					//
					if (p[ii].ParameterType.Resolve () != args[ii].Type.Resolve ())
						match = false;
				}

				if (match)
					return m;
			}

			return null;
		}

		void ReadCustomAttributeProperties (XPathNodeIterator iterator, TypeDefinition attributeType, CustomAttribute customAttribute)
		{
			while (iterator.MoveNext ()) {
				string propertyName = GetName (iterator.Current);
				if (string.IsNullOrEmpty (propertyName)) {
					LogWarning ($"Property element does not contain attribute 'name'", 2051, iterator.Current);
					continue;
				}

				PropertyDefinition property = attributeType.Properties.Where (prop => prop.Name == propertyName).FirstOrDefault ();
				if (property == null) {
					LogWarning ($"Property '{propertyName}' could not be found", 2052, iterator.Current);
					continue;
				}

				var caa = ReadCustomAttributeArgument (iterator, property);
				if (caa is null)
					continue;

				customAttribute.Properties.Add (new CustomAttributeNamedArgument (property.Name, caa.Value));
			}
		}

		CustomAttributeArgument[] ReadCustomAttributeArguments (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			var args = new ArrayBuilder<CustomAttributeArgument> ();

			iterator = iterator.Current.SelectChildren ("argument", string.Empty);
			while (iterator.MoveNext ()) {
				CustomAttributeArgument? caa = ReadCustomAttributeArgument (iterator, attributeType);
				if (caa is not null)
					args.Add (caa.Value);
			}

			return args.ToArray () ?? Array.Empty<CustomAttributeArgument> ();
		}

		CustomAttributeArgument? ReadCustomAttributeArgument (XPathNodeIterator iterator, IMemberDefinition memberWithAttribute)
		{
			TypeReference typeref = ResolveArgumentType (iterator, memberWithAttribute);
			if (typeref is null)
				return null;

			string svalue = iterator.Current.Value;

			//
			// Builds CustomAttributeArgument in the same way as it would be
			// represented in the metadata if encoded there. This simplifies
			// any custom attributes handling in linker by using same attributes
			// value extraction or mathing logic.
			//
			switch (typeref.MetadataType) {
			case MetadataType.Object:
				iterator = iterator.Current.SelectChildren ("argument", string.Empty);
				if (iterator?.MoveNext () != true) {
					_context.LogError ($"Custom attribute argument for 'System.Object' requires nested 'argument' node", 1043);
					return null;
				}

				var boxedValue = ReadCustomAttributeArgument (iterator, typeref.Resolve ());
				if (boxedValue is null)
					return null;

				return new CustomAttributeArgument (typeref, boxedValue);

			case MetadataType.Char:
			case MetadataType.Byte:
			case MetadataType.SByte:
			case MetadataType.Int16:
			case MetadataType.UInt16:
			case MetadataType.Int32:
			case MetadataType.UInt32:
			case MetadataType.UInt64:
			case MetadataType.Int64:
			case MetadataType.String:
				return new CustomAttributeArgument (typeref, ConvertStringValue (svalue, typeref));

			case MetadataType.ValueType:
				var enumType = _context.Resolve (typeref);
				if (enumType?.IsEnum != true)
					goto default;

				var enumField = enumType.Fields.Where (f => f.IsStatic && f.Name == svalue).FirstOrDefault ();
				object evalue = enumField?.Constant ?? svalue;

				typeref = enumType.GetEnumUnderlyingType ();
				return new CustomAttributeArgument (enumType, ConvertStringValue (evalue, typeref));

			case MetadataType.Class:
				if (!typeref.IsTypeOf ("System", "Type"))
					goto default;

				TypeReference type = _context.TypeNameResolver.ResolveTypeName (svalue, memberWithAttribute, out _);
				if (type == null) {
					_context.LogError ($"Could not resolve custom attribute type value '{svalue}'", 1044, origin: GetMessageOriginForPosition (iterator.Current));
					return null;
				}

				return new CustomAttributeArgument (typeref, type);
			default:
				// No support for null and arrays, consider adding - mono/linker/issues/1957
				_context.LogError ($"Unexpected attribute argument type '{typeref.GetDisplayName ()}'", 1045);
				return null;
			}

			TypeReference ResolveArgumentType (XPathNodeIterator iterator, IMemberDefinition memberWithAttribute)
			{
				string typeName = GetAttribute (iterator.Current, "type");
				if (string.IsNullOrEmpty (typeName))
					typeName = "System.String";

				TypeReference typeref = _context.TypeNameResolver.ResolveTypeName (typeName, memberWithAttribute, out _);
				if (typeref == null) {
					_context.LogError ($"The type '{typeName}' used with attribute value '{iterator.Current.Value}' could not be found", 1041, origin: GetMessageOriginForPosition (iterator.Current));
					return null;
				}

				return typeref;
			}
		}

		object ConvertStringValue (object value, TypeReference targetType)
		{
			TypeCode typeCode;
			switch (targetType.MetadataType) {
			case MetadataType.String:
				typeCode = TypeCode.String;
				break;
			case MetadataType.Char:
				typeCode = TypeCode.Char;
				break;
			case MetadataType.Byte:
				typeCode = TypeCode.Byte;
				break;
			case MetadataType.SByte:
				typeCode = TypeCode.SByte;
				break;
			case MetadataType.Int16:
				typeCode = TypeCode.Int16;
				break;
			case MetadataType.UInt16:
				typeCode = TypeCode.UInt16;
				break;
			case MetadataType.Int32:
				typeCode = TypeCode.Int32;
				break;
			case MetadataType.UInt32:
				typeCode = TypeCode.UInt32;
				break;
			case MetadataType.UInt64:
				typeCode = TypeCode.UInt64;
				break;
			case MetadataType.Int64:
				typeCode = TypeCode.Int64;
				break;
			case MetadataType.Boolean:
				typeCode = TypeCode.Boolean;
				break;
			case MetadataType.Single:
				typeCode = TypeCode.Single;
				break;
			case MetadataType.Double:
				typeCode = TypeCode.Double;
				break;
			default:
				throw new NotSupportedException (targetType.ToString ());
			}

			try {
				return Convert.ChangeType (value, typeCode);
			} catch {
				_context.LogError ($"Cannot convert value '{value}' to type '{targetType.GetDisplayName ()}'", 1042);
				return null;
			}
		}

		bool GetAttributeType (XPathNodeIterator iterator, string attributeFullName, out TypeDefinition attributeType)
		{
			string assemblyName = GetAttribute (iterator.Current, "assembly");
			if (string.IsNullOrEmpty (assemblyName)) {
				attributeType = _context.GetType (attributeFullName);
			} else {
				AssemblyDefinition assembly;
				try {
					assembly = _context.TryResolve (AssemblyNameReference.Parse (assemblyName));
					if (assembly == null) {
						LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, iterator.Current);
						attributeType = default;
						return false;
					}
				} catch (Exception) {
					LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, iterator.Current);
					attributeType = default;
					return false;
				}

				attributeType = _context.TryResolve (assembly, attributeFullName);
			}

			if (attributeType == null) {
				LogWarning ($"Attribute type '{attributeFullName}' could not be found", 2031, iterator.Current);
				return false;
			}

			return true;
		}

		protected override AllowedAssemblies AllowedAssemblySelector {
			get {
				if (_resourceAssembly == null)
					return AllowedAssemblies.AllAssemblies;

				// Corelib XML may contain assembly wildcard to support compiler-injected attribute types
				if (_resourceAssembly.Name.Name == PlatformAssemblies.CoreLib)
					return AllowedAssemblies.AllAssemblies;

				return AllowedAssemblies.ContainingAssembly;
			}
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			PopulateAttributeInfo (assembly, nav);
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
		}

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			PopulateAttributeInfo (type, nav);
			ProcessTypeChildren (type, nav);

			if (!type.HasNestedTypes)
				return;

			var iterator = nav.SelectChildren ("type", string.Empty);
			while (iterator.MoveNext ()) {
				foreach (TypeDefinition nested in type.NestedTypes) {
					if (nested.Name == GetAttribute (iterator.Current, "name") && ShouldProcessElement (iterator.Current))
						ProcessType (nested, iterator.Current);
				}
			}
		}

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			PopulateAttributeInfo (field, nav);
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object customData)
		{
			PopulateAttributeInfo (method, nav);
			ProcessReturnParameters (method, nav);
			ProcessParameters (method, nav);
		}

		void ProcessParameters (MethodDefinition method, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren ("parameter", string.Empty);
			while (iterator.MoveNext ()) {
				var attributes = ProcessAttributes (iterator.Current, method);
				if (attributes != null) {
					string paramName = GetAttribute (iterator.Current, "name");
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (parameter.HasCustomAttributes || _attributeInfo.CustomAttributes.ContainsKey (parameter))
								LogWarning (
									$"More than one value specified for parameter '{paramName}' of method '{method.GetDisplayName ()}'",
									2024, iterator.Current);
							_attributeInfo.AddCustomAttributes (parameter, attributes);
							break;
						}
					}
				}
			}
		}

		void ProcessReturnParameters (MethodDefinition method, XPathNavigator nav)
		{
			var iterator = nav.SelectChildren ("return", string.Empty);
			bool firstAppearance = true;
			while (iterator.MoveNext ()) {
				if (firstAppearance) {
					firstAppearance = false;
					PopulateAttributeInfo (method.MethodReturnType, iterator.Current);
				} else {
					LogWarning (
						$"There is more than one 'return' child element specified for method '{method.GetDisplayName ()}'",
						2023, iterator.Current);
				}
			}
		}

		protected override MethodDefinition GetMethod (TypeDefinition type, string signature)
		{
			if (type.HasMethods)
				foreach (MethodDefinition method in type.Methods)
					if (signature.Replace (" ", "") == GetMethodSignature (method) || signature.Replace (" ", "") == GetMethodSignature (method, true))
						return method;

			return null;
		}

		static string GetMethodSignature (MethodDefinition method, bool includeReturnType = false)
		{
			StringBuilder sb = new StringBuilder ();
			if (includeReturnType) {
				sb.Append (method.ReturnType.FullName);
			}
			sb.Append (method.Name);
			if (method.HasGenericParameters) {
				sb.Append ("<");
				for (int i = 0; i < method.GenericParameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (method.GenericParameters[i].Name);
				}
				sb.Append (">");
			}
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count; i++) {
					if (i > 0)
						sb.Append (",");

					sb.Append (method.Parameters[i].ParameterType.FullName);
				}
			}
			sb.Append (")");
			return sb.ToString ();
		}

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object customData, bool fromSignature)
		{
			PopulateAttributeInfo (property, nav);
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object customData)
		{
			PopulateAttributeInfo (@event, nav);
		}

		void PopulateAttributeInfo (ICustomAttributeProvider provider, XPathNavigator nav)
		{
			var attributes = ProcessAttributes (nav, provider);
			if (attributes != null)
				_attributeInfo.AddCustomAttributes (provider, attributes);
		}
	}
}