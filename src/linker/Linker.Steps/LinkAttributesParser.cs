// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LinkAttributesParser : ProcessLinkerXmlBase
	{
		AttributeInfo _attributeInfo;

		public LinkAttributesParser (LinkContext context, XPathDocument document, string xmlDocumentLocation)
			: base (context, document, xmlDocumentLocation)
		{
		}

		public LinkAttributesParser (LinkContext context, XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "<unspecified>")
			: base (context, document, resource, resourceAssembly, xmlDocumentLocation)
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
						_context.LogWarning ($"Internal attribute '{attributeType.Name}' can only be used on attribute types", 2048, _xmlDocumentLocation);
						continue;
					}
				} else {
					string attributeFullName = GetFullName (iterator.Current);
					if (string.IsNullOrEmpty (attributeFullName)) {
						_context.LogWarning ($"'attribute' element does not contain attribute 'fullname' or it's empty", 2029, _xmlDocumentLocation);
						continue;
					}

					if (!GetAttributeType (iterator, attributeFullName, out attributeType))
						continue;
				}

				CustomAttribute ca = CreateCustomAttribute (iterator, attributeType);
				if (ca != null)
					builder.Add (ca);
			}

			return builder.ToArray ();
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

			//
			// Generates metadata information for internal type
			//
			// public sealed class RemoveAttributeInstancesAttribute : Attribute
			// {
			//		public RemoveAttributeInstancesAttribute () {}
			// }
			//
			var td = new TypeDefinition ("", "RemoveAttributeInstancesAttribute", TypeAttributes.Public);
			td.BaseType = attributeType;

			const MethodAttributes ctorAttributes = MethodAttributes.Public | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName | MethodAttributes.Final;
			var ctor = new MethodDefinition (".ctor", ctorAttributes, voidType);
			td.Methods.Add (ctor);

			return _context.MarkedKnownMembers.RemoveAttributeInstancesAttributeDefinition = td;
		}

		CustomAttribute CreateCustomAttribute (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			string[] attributeArguments = GetAttributeChildren (iterator.Current.SelectChildren ("argument", string.Empty)).ToArray ();
			var attributeArgumentCount = attributeArguments == null ? 0 : attributeArguments.Length;
			MethodDefinition constructor = attributeType.Methods.Where (method => method.IsInstanceConstructor ()).FirstOrDefault (c => c.Parameters.Count == attributeArgumentCount);
			if (constructor == null) {
				_context.LogWarning (
					$"Could not find a constructor for type '{attributeType}' that has '{attributeArgumentCount}' arguments",
					2022,
					_xmlDocumentLocation);
				return null;
			}

			CustomAttribute customAttribute = new CustomAttribute (constructor);
			var arguments = ProcessAttributeArguments (constructor, attributeArguments);
			if (arguments != null)
				foreach (var argument in arguments)
					customAttribute.ConstructorArguments.Add (argument);

			var properties = ProcessAttributeProperties (iterator.Current.SelectChildren ("property", string.Empty), attributeType);
			foreach (var property in properties)
				customAttribute.Properties.Add (property);

			return customAttribute;
		}

		List<CustomAttributeNamedArgument> ProcessAttributeProperties (XPathNodeIterator iterator, TypeDefinition attributeType)
		{
			List<CustomAttributeNamedArgument> attributeProperties = new List<CustomAttributeNamedArgument> ();
			while (iterator.MoveNext ()) {
				string propertyName = GetName (iterator.Current);
				if (string.IsNullOrEmpty (propertyName)) {
					_context.LogWarning ($"Property element does not contain attribute 'name'", 2051, _xmlDocumentLocation);
					continue;
				}

				PropertyDefinition property = attributeType.Properties.Where (prop => prop.Name == propertyName).FirstOrDefault ();
				if (property == null) {
					_context.LogWarning ($"Property '{propertyName}' could not be found", 2052, _xmlDocumentLocation);
					continue;
				}

				var propertyValue = iterator.Current.Value;
				if (!TryConvertValue (propertyValue, property.PropertyType, out object value)) {
					_context.LogWarning ($"Invalid value '{propertyValue}' for property '{propertyName}'", 2053, _xmlDocumentLocation);
					continue;
				}

				attributeProperties.Add (new CustomAttributeNamedArgument (property.Name,
					new CustomAttributeArgument (property.PropertyType, value)));
			}

			return attributeProperties;
		}

		List<CustomAttributeArgument> ProcessAttributeArguments (MethodDefinition attributeConstructor, string[] arguments)
		{
			if (arguments == null)
				return null;

			List<CustomAttributeArgument> attributeArguments = new List<CustomAttributeArgument> ();
			for (int i = 0; i < arguments.Length; i++) {
				object argValue;
				TypeDefinition parameterType = attributeConstructor.Parameters[i].ParameterType.Resolve ();
				if (!TryConvertValue (arguments[i], parameterType, out argValue)) {
					_context.LogWarning (
						$"Invalid argument value '{arguments[i]}' for parameter type '{parameterType.GetDisplayName ()}' of attribute '{attributeConstructor.DeclaringType.GetDisplayName ()}'",
						2054,
						_xmlDocumentLocation);
					return null;
				}

				attributeArguments.Add (new CustomAttributeArgument (parameterType, argValue));
			}

			return attributeArguments;
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
						_context.LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, _xmlDocumentLocation);
						attributeType = default;
						return false;
					}
				} catch (Exception) {
					_context.LogWarning ($"Could not resolve assembly '{assemblyName}' for attribute '{attributeFullName}'", 2030, _xmlDocumentLocation);
					attributeType = default;
					return false;
				}

				attributeType = _context.TypeNameResolver.ResolveTypeName (assembly, attributeFullName)?.Resolve ();
			}

			if (attributeType == null) {
				_context.LogWarning ($"Attribute type '{attributeFullName}' could not be found", 2031, _xmlDocumentLocation);
				return false;
			}

			return true;
		}

		static ArrayBuilder<string> GetAttributeChildren (XPathNodeIterator iterator)
		{
			ArrayBuilder<string> children = new ArrayBuilder<string> ();
			while (iterator.MoveNext ()) {
				children.Add (iterator.Current.Value);
			}
			return children;
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
								_context.LogWarning (
									$"More than one value specified for parameter '{paramName}' of method '{method.GetDisplayName ()}'",
									2024, _xmlDocumentLocation);
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
					_context.LogWarning (
						$"There is more than one 'return' child element specified for method '{method.GetDisplayName ()}'",
						2023, _xmlDocumentLocation);
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