// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LinkAttributesParser : ProcessLinkerXmlBase
	{
		AttributeInfo? _attributeInfo;

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
			bool stripLinkAttributes = _context.IsOptimizationEnabled (CodeOptimizations.RemoveLinkAttributes, _resource?.Assembly);
			ProcessXml (stripLinkAttributes, _context.IgnoreLinkAttributes);
		}
		TypeDefinition? _paramArrayAttributeType;
		TypeDefinition? ParamArrayAttributeType {
			get {
				if (_paramArrayAttributeType is TypeDefinition td)
					return td;
				_paramArrayAttributeType = BCL.FindPredefinedType ("System", "ParamArrayAttribute", _context);
				return _paramArrayAttributeType;
			}
		}

		CustomAttribute? _paramArrayCustomAttribute;
		CustomAttribute? ParamArrayCustomAttribute {
			get {
				if (_paramArrayCustomAttribute is CustomAttribute ca)
					return ca;
				else {
					var paramArrayAttributeType = BCL.FindPredefinedType ("System", "ParamArrayAttribute", _context);
					if (paramArrayAttributeType == null)
						return null;
					_paramArrayCustomAttribute = new CustomAttribute (paramArrayAttributeType.GetDefaultInstanceConstructor ());
					return _paramArrayCustomAttribute;
				}
			}
		}

		static bool IsRemoveAttributeInstances (string attributeName) => attributeName == "RemoveAttributeInstances" || attributeName == "RemoveAttributeInstancesAttribute";

		CustomAttribute[]? ProcessAttributes (XPathNavigator nav, ICustomAttributeProvider provider)
		{
			var builder = new ArrayBuilder<CustomAttribute> ();
			foreach (XPathNavigator attributeNav in nav.SelectChildren ("attribute", string.Empty)) {
				if (!ShouldProcessElement (attributeNav))
					continue;

				TypeDefinition? attributeType;
				string internalAttribute = GetAttribute (attributeNav, "internal");
				if (!string.IsNullOrEmpty (internalAttribute)) {
					if (!IsRemoveAttributeInstances (internalAttribute)) {
						LogWarning (attributeNav, DiagnosticId.UnrecognizedInternalAttribute, internalAttribute);
						continue;
					}
					// TODO: Replace with IsAttributeType check once we have it
					if (provider is not TypeDefinition) {
						LogWarning (attributeNav, DiagnosticId.XmlRemoveAttributeInstancesCanOnlyBeUsedOnType, nameof (RemoveAttributeInstancesAttribute));
						continue;
					}

					var argCount = attributeNav.SelectChildren ("argument", string.Empty).Count;
					attributeType = GenerateRemoveAttributeInstancesAttribute (argCount);
					if (attributeType == null)
						continue;
				} else {
					string attributeFullName = GetFullName (attributeNav);
					if (string.IsNullOrEmpty (attributeFullName)) {
						LogWarning (attributeNav, DiagnosticId.XmlElementDoesNotContainRequiredAttributeFullname);
						continue;
					}

					if (!GetAttributeType (attributeNav, attributeFullName, out attributeType))
						continue;
				}

				CustomAttribute? customAttribute = CreateCustomAttribute (attributeNav, attributeType);
				if (customAttribute != null) {
					_context.LogMessage ($"Assigning external custom attribute '{FormatCustomAttribute (customAttribute)}' instance to '{provider}'.");
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
		///	<summary>
		/// Here, we generate new constructors for the synthetic RemoveAttributeInstancesAttribute type
		///	(in the Cecil space, with no namespace) based on the number of arguments in the XML.
		///	Each such .ctor is defined as .ctor(object, object, ....) - all arguments are boxed
		///	in the XML by wrapping them with "object" argument.
		/// The internal linker representation which is a type called 
		/// <see cref="Mono.Linker.RemoveAttributeInstancesAttribute">Mono.Linker.RemoveAttributeInstancesAttribute</see>
		///	which has only one .ctor which takes a collection of arguments (<c>Collection&lt;CustomAttributeArgument&gt;</c>).
		///	The linker attribute caching mechanism <see cref="Mono.Linker.LinkerAttributesInformation"/> converts the call to 
		///	this generated .ctor with arbitrary number of arguments to the instantiation of the internal representation.
		///	The boxing within an Object is removed in this conversion.
		///	</summary>
		TypeDefinition? GenerateRemoveAttributeInstancesAttribute (int ctorArgumentCount)
		{
			TypeDefinition? td = null;

			if (_context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition is TypeDefinition knownTypeDef) {
				return knownTypeDef;
			}

			var voidType = BCL.FindPredefinedType ("System", "Void", _context);
			if (voidType == null)
				return null;

			var attributeType = BCL.FindPredefinedType ("System", "Attribute", _context);
			if (attributeType == null)
				return null;

			var objectType = BCL.FindPredefinedType ("System", "Object", _context);
			if (objectType == null)
				return null;
			var objectArrayType = new ArrayType (objectType);
			if (objectArrayType == null)
				return null;

			//
			// Generates metadata information for internal type
			//
			// public sealed class RemoveAttributeInstancesAttribute : Attribute
			// {
			//		public RemoveAttributeInstancesAttribute () {}
			//		public RemoveAttributeInstancesAttribute (params object[] values) {}
			// }
			//
			const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Final;

			if (td == null) {
				td = new TypeDefinition ("", nameof (RemoveAttributeInstancesAttribute), TypeAttributes.Public);

				td.BaseType = attributeType;
			}

			var ctorN = new MethodDefinition (".ctor", ctorAttributes, voidType);
			var param = new ParameterDefinition (objectArrayType);
			param.CustomAttributes.Add (ParamArrayCustomAttribute);
			ctorN.Parameters.Add (param);
			td.Methods.Add (ctorN);

			return _context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition = td;
		}

		CustomAttribute? CreateCustomAttribute (XPathNavigator nav, TypeDefinition attributeType)
		{
			CustomAttributeArgument[] arguments = ReadCustomAttributeArguments (nav, attributeType);

			MethodDefinition? constructor = FindBestMatchingConstructor (attributeType, arguments);
			if (constructor == null) {
				LogWarning (nav, DiagnosticId.XmlCouldNotFindMatchingConstructorForCustomAttribute, attributeType.GetDisplayName ());
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			foreach (var argument in arguments)
				customAttribute.ConstructorArguments.Add (argument);

			ReadCustomAttributeProperties (nav, attributeType, customAttribute);

			return customAttribute;
		}

		MethodDefinition? FindBestMatchingConstructor (TypeDefinition attributeType, CustomAttributeArgument[] args)
		{
			var methods = attributeType.Methods;
			for (int i = 0; i < attributeType.Methods.Count; ++i) {
				var method = methods[i];
				if (!method.IsInstanceConstructor ())
					continue;

				var parameters = method.Parameters;
				if (args.Length != parameters.Count
					&& !(parameters.Count > 0
						&& method.Parameters.Last ().CustomAttributes
							.Any (ca => _context.Resolve (ca.AttributeType) == ParamArrayAttributeType)))
					continue;

				bool match = true;
				for (int ii = 0; match && ii < args.Length; ++ii) {
					//
					// No candidates betterness, only exact matches are supported
					//
					var parameterType = _context.TryResolve (parameters[ii].ParameterType);
					if (parameterType == null)
						match = false;
					// Account for param array constructors (params T[] arg)
					else if (parameters[ii].CustomAttributes.Any (ca => _context.Resolve (ca.AttributeType) == ParamArrayAttributeType)
							&& parameters[ii].ParameterType.MetadataType == MetadataType.Array) {
						for (; ii < args.Length; ii++) {
							if (parameterType != _context.TryResolve (args[ii].Type)) {
								match = false;
							}
						}
					} else if (parameterType != _context.TryResolve (args[ii].Type))
						match = false;
				}

				if (match)
					return method;
			}

			return null;
		}

		void ReadCustomAttributeProperties (XPathNavigator nav, TypeDefinition attributeType, CustomAttribute customAttribute)
		{
			foreach (XPathNavigator propertyNav in nav.SelectChildren ("property", string.Empty)) {
				string propertyName = GetName (propertyNav);
				if (string.IsNullOrEmpty (propertyName)) {
					LogWarning (propertyNav, DiagnosticId.XmlPropertyDoesNotContainAttributeName);
					continue;
				}

				PropertyDefinition? property = attributeType.Properties.Where (prop => prop.Name == propertyName).FirstOrDefault ();
				if (property == null) {
					LogWarning (propertyNav, DiagnosticId.XmlCouldNotFindProperty, propertyName);
					continue;
				}

				var caa = ReadCustomAttributeArgument (propertyNav, property);
				if (caa is null)
					continue;

				customAttribute.Properties.Add (new CustomAttributeNamedArgument (property.Name, caa.Value));
			}
		}

		CustomAttributeArgument[] ReadCustomAttributeArguments (XPathNavigator nav, TypeDefinition attributeType)
		{
			var args = new ArrayBuilder<CustomAttributeArgument> ();

			foreach (XPathNavigator argumentNav in nav.SelectChildren ("argument", string.Empty)) {
				CustomAttributeArgument? caa = ReadCustomAttributeArgument (argumentNav, attributeType);
				if (caa is not null)
					args.Add (caa.Value);
			}

			return args.ToArray () ?? Array.Empty<CustomAttributeArgument> ();
		}

		CustomAttributeArgument? ReadCustomAttributeArgument (XPathNavigator nav, IMemberDefinition memberWithAttribute)
		{
			TypeReference? typeref = ResolveArgumentType (nav, memberWithAttribute);
			if (typeref is null)
				return null;

			string svalue = nav.Value;

			//
			// Builds CustomAttributeArgument in the same way as it would be
			// represented in the metadata if encoded there. This simplifies
			// any custom attributes handling in linker by using same attributes
			// value extraction or mathing logic.
			//
			switch (typeref.MetadataType) {
			case MetadataType.Object:
				var argumentIterator = nav.SelectChildren ("argument", string.Empty);
				if (argumentIterator?.MoveNext () != true) {
					_context.LogError (null, DiagnosticId.CustomAttributeArgumentForTypeRequiresNestedNode, "System.Object", "argument");
					return null;
				}

				var typedef = _context.TryResolve (typeref);
				if (typedef == null)
					return null;

				var boxedValue = ReadCustomAttributeArgument (argumentIterator.Current!, typedef);
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

				if (!_context.TypeNameResolver.TryResolveTypeName (svalue, memberWithAttribute, out TypeReference? type, out _)) {
					_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.CouldNotResolveCustomAttributeTypeValue, svalue);
					return null;
				}

				return new CustomAttributeArgument (typeref, type);
			default:
				// No support for null and arrays, consider adding - dotnet/linker/issues/1957
				_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.UnexpectedAttributeArgumentType, typeref.GetDisplayName ());
				return null;
			}

			TypeReference? ResolveArgumentType (XPathNavigator nav, IMemberDefinition memberWithAttribute)
			{
				string typeName = GetAttribute (nav, "type");
				if (string.IsNullOrEmpty (typeName))
					typeName = "System.String";

				if (!_context.TypeNameResolver.TryResolveTypeName (typeName, memberWithAttribute, out TypeReference? typeref, out _)) {
					_context.LogError (GetMessageOriginForPosition (nav), DiagnosticId.TypeUsedWithAttributeValueCouldNotBeFound, typeName, nav.Value);
					return null;
				}

				return typeref;
			}
		}

		object? ConvertStringValue (object value, TypeReference targetType)
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
				_context.LogError (null, DiagnosticId.CannotConverValueToType, value.ToString () ?? "", targetType.GetDisplayName ());
				return null;
			}
		}

		bool GetAttributeType (XPathNavigator nav, string attributeFullName, [NotNullWhen (true)] out TypeDefinition? attributeType)
		{
			string assemblyName = GetAttribute (nav, "assembly");
			if (string.IsNullOrEmpty (assemblyName)) {
				attributeType = _context.GetType (attributeFullName);
			} else {
				AssemblyDefinition? assembly;
				try {
					assembly = _context.TryResolve (AssemblyNameReference.Parse (assemblyName));
					if (assembly == null) {
						LogWarning (nav, DiagnosticId.XmlCouldNotResolveAssemblyForAttribute, assemblyName, attributeFullName);

						attributeType = default;
						return false;
					}
				} catch (Exception) {
					LogWarning (nav, DiagnosticId.XmlCouldNotResolveAssemblyForAttribute, assemblyName, attributeFullName);
					attributeType = default;
					return false;
				}

				attributeType = _context.TryResolve (assembly, attributeFullName);
			}

			if (attributeType == null) {
				LogWarning (nav, DiagnosticId.XmlAttributeTypeCouldNotBeFound, attributeFullName);
				return false;
			}

			return true;
		}

		protected override AllowedAssemblies AllowedAssemblySelector {
			get {
				if (_resource?.Assembly == null)
					return AllowedAssemblies.AllAssemblies;

				// Corelib XML may contain assembly wildcard to support compiler-injected attribute types
				if (_resource?.Assembly.Name.Name == PlatformAssemblies.CoreLib)
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

			foreach (XPathNavigator nestedTypeNav in nav.SelectChildren ("type", string.Empty)) {
				foreach (TypeDefinition nested in type.NestedTypes) {
					if (nested.Name == GetAttribute (nestedTypeNav, "name") && ShouldProcessElement (nestedTypeNav))
						ProcessType (nested, nestedTypeNav);
				}
			}
		}

		protected override void ProcessField (TypeDefinition type, FieldDefinition field, XPathNavigator nav)
		{
			PopulateAttributeInfo (field, nav);
		}

		protected override void ProcessMethod (TypeDefinition type, MethodDefinition method, XPathNavigator nav, object? customData)
		{
			PopulateAttributeInfo (method, nav);
			ProcessReturnParameters (method, nav);
			ProcessParameters (method, nav);
		}

		void ProcessParameters (MethodDefinition method, XPathNavigator nav)
		{
			Debug.Assert (_attributeInfo != null);
			foreach (XPathNavigator parameterNav in nav.SelectChildren ("parameter", string.Empty)) {
				var attributes = ProcessAttributes (parameterNav, method);
				if (attributes != null) {
					string paramName = GetAttribute (parameterNav, "name");
					foreach (ParameterDefinition parameter in method.Parameters) {
						if (paramName == parameter.Name) {
							if (parameter.HasCustomAttributes || _attributeInfo.CustomAttributes.ContainsKey (parameter))
								LogWarning (parameterNav, DiagnosticId.XmlMoreThanOneValyForParameterOfMethod, paramName, method.GetDisplayName ());
							_attributeInfo.AddCustomAttributes (parameter, attributes);
							break;
						}
					}
				}
			}
		}

		void ProcessReturnParameters (MethodDefinition method, XPathNavigator nav)
		{
			bool firstAppearance = true;
			foreach (XPathNavigator returnNav in nav.SelectChildren ("return", string.Empty)) {
				if (firstAppearance) {
					firstAppearance = false;
					PopulateAttributeInfo (method.MethodReturnType, returnNav);
				} else {
					LogWarning (returnNav, DiagnosticId.XmlMoreThanOneReturnElementForMethod, method.GetDisplayName ());
				}
			}
		}

		protected override MethodDefinition? GetMethod (TypeDefinition type, string signature)
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

		protected override void ProcessProperty (TypeDefinition type, PropertyDefinition property, XPathNavigator nav, object? customData, bool fromSignature)
		{
			PopulateAttributeInfo (property, nav);
		}

		protected override void ProcessEvent (TypeDefinition type, EventDefinition @event, XPathNavigator nav, object? customData)
		{
			PopulateAttributeInfo (@event, nav);
		}

		void PopulateAttributeInfo (ICustomAttributeProvider provider, XPathNavigator nav)
		{
			Debug.Assert (_attributeInfo != null);
			var attributes = ProcessAttributes (nav, provider);
			if (attributes != null)
				_attributeInfo.AddCustomAttributes (provider, attributes);
		}
	}
}