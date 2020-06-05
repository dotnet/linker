﻿using System;
using System.Linq;
using System.Globalization;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : BaseStep
	{
		readonly XPathDocument _document;
		readonly string _xmlDocumentLocation;
		readonly string _resourceName;
		readonly AssemblyDefinition _resourceAssembly;

		public BodySubstituterStep (XPathDocument document, string xmlDocumentLocation)
		{
			_document = document;
			_xmlDocumentLocation = xmlDocumentLocation;
		}

		public BodySubstituterStep (XPathDocument document, string resourceName, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "")
			: this (document, xmlDocumentLocation)
		{
			if (string.IsNullOrEmpty (resourceName))
				throw new ArgumentNullException (nameof (resourceName));

			_resourceName = resourceName;
			_resourceAssembly = resourceAssembly ?? throw new ArgumentNullException (nameof (resourceAssembly));
		}

		protected override void Process ()
		{
			if (!string.IsNullOrEmpty (_resourceName) && Context.StripSubstitutions)
				Context.Annotations.AddResourceToRemove (_resourceAssembly, _resourceName);

			if (!string.IsNullOrEmpty (_resourceName) && Context.IgnoreSubstitutions)
				return;

			ReadSubstitutions (_document);
		}

		bool ShouldProcessSubstitutions (XPathNavigator nav)
		{
			var feature = GetAttribute (nav, "feature");
			if (string.IsNullOrEmpty (feature))
				return true;

			var value = GetAttribute (nav, "featurevalue");
			if (string.IsNullOrEmpty (value)) {
				Context.LogError ($"Failed to process XML substitution: '{_xmlDocumentLocation}'. Feature {feature} does not specify a 'featurevalue' attribute", 1001);
				return false;
			}

			if (!bool.TryParse (value, out bool bValue)) {
				Context.LogError ($"Failed to process XML substitution: '{_xmlDocumentLocation}'. Unsupported non-boolean feature definition {feature}", 1002);
				return false;
			}

			if (Context.FeatureSettings == null || !Context.FeatureSettings.TryGetValue (feature, out bool featureSetting))
				return false;

			return bValue == featureSetting;
		}

		void ReadSubstitutions (XPathDocument document)
		{
			XPathNavigator nav = document.CreateNavigator ();

			// Initial structure check
			if (!nav.MoveToChild ("linker", ""))
				return;

			if (!ShouldProcessSubstitutions (nav))
				return;

			ProcessAssemblies (nav.SelectChildren ("assembly", ""));
		}

		void ProcessAssemblies (XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				var name = GetAssemblyName (iterator.Current);

				if (!ShouldProcessSubstitutions (iterator.Current))
					continue;

				AssemblyDefinition assembly = Context.GetLoadedAssembly (name.Name);

				if (assembly == null) {
					Context.LogWarning ($"Could not resolve assembly {GetAssemblyName (iterator.Current).Name} specified in {_xmlDocumentLocation}", 2007, _xmlDocumentLocation);
					continue;
				}

				ProcessAssembly (assembly, iterator);
			}
		}

		void ProcessAssembly (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			ProcessTypes (assembly, iterator.Current.SelectChildren ("type", ""));
		}

		void ProcessTypes (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				if (!ShouldProcessSubstitutions (nav))
					continue;

				string fullname = GetAttribute (nav, "fullname");

				TypeDefinition type = assembly.MainModule.GetType (fullname);

				if (type == null) {
					Context.LogWarning ($"Could not resolve type '{fullname}' specified in {_xmlDocumentLocation}", 2008, _xmlDocumentLocation);
					continue;
				}

				ProcessType (type, nav);
			}
		}

		void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			if (!nav.HasChildren)
				return;

			if (!ShouldProcessSubstitutions (nav))
				return;

			XPathNodeIterator methods = nav.SelectChildren ("method", "");
			if (methods.Count > 0)
				ProcessMethods (type, methods);

			var fields = nav.SelectChildren ("field", "");
			if (fields.Count > 0) {
				while (fields.MoveNext ()) {
					if (!ShouldProcessSubstitutions (fields.Current))
						continue;

					ProcessField (type, fields);
				}
			}
		}

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				if (!ShouldProcessSubstitutions (iterator.Current))
					continue;

				ProcessMethod (type, iterator);
			}
		}

		void ProcessMethod (TypeDefinition type, XPathNodeIterator iterator)
		{
			string signature = GetAttribute (iterator.Current, "signature");
			if (string.IsNullOrEmpty (signature))
				return;

			MethodDefinition method = FindMethod (type, signature);
			if (method == null) {
				Context.LogWarning ($"Could not find method '{signature}' in type '{type.FullName}' specified in {_xmlDocumentLocation}", 2009, _xmlDocumentLocation);
				return;
			}

			string action = GetAttribute (iterator.Current, "body");
			switch (action) {
			case "remove":
				Annotations.SetAction (method, MethodAction.ConvertToThrow);
				return;
			case "stub":
				string value = GetAttribute (iterator.Current, "value");
				if (value != "") {
					if (!TryConvertValue (value, method.ReturnType, out object res)) {
						Context.LogWarning ($"Invalid value for '{method.GetName ()}' stub", 2010, _xmlDocumentLocation);
						return;
					}

					Annotations.SetMethodStubValue (method, res);
				}

				Annotations.SetAction (method, MethodAction.ConvertToStub);
				return;
			default:
				Context.LogWarning ($"Unknown body modification '{action}' for '{method.GetName ()}'", 2011, _xmlDocumentLocation);
				return;
			}
		}

		void ProcessField (TypeDefinition type, XPathNodeIterator iterator)
		{
			string name = GetAttribute (iterator.Current, "name");
			if (string.IsNullOrEmpty (name))
				return;

			var field = type.Fields.FirstOrDefault (f => f.Name == name);
			if (field == null) {
				Context.LogWarning ($"Could not find field '{name}' in type '{type.FullName}' specified in { _xmlDocumentLocation}", 2012, _xmlDocumentLocation);
				return;
			}

			if (!field.IsStatic || field.IsLiteral) {
				Context.LogWarning ($"Substituted field '{name}' needs to be static field.", 2013, _xmlDocumentLocation);
				return;
			}

			string value = GetAttribute (iterator.Current, "value");
			if (string.IsNullOrEmpty (value)) {
				Context.LogWarning ($"Missing 'value' attribute for field '{field}'.", 2014, _xmlDocumentLocation);
				return;
			}
			if (!TryConvertValue (value, field.FieldType, out object res)) {
				Context.LogWarning ($"Invalid value for '{field}': '{value}'.", 2015, _xmlDocumentLocation);
				return;
			}

			Annotations.SetFieldValue (field, res);

			string init = GetAttribute (iterator.Current, "initialize");
			if (init?.ToLowerInvariant () == "true") {
				Annotations.SetSubstitutedInit (field);
			}
		}

		static bool TryConvertValue (string value, TypeReference target, out object result)
		{
			switch (target.MetadataType) {
			case MetadataType.Boolean:
				if (bool.TryParse (value, out bool bvalue)) {
					result = bvalue ? 1 : 0;
					return true;
				}

				goto case MetadataType.Int32;

			case MetadataType.Byte:
				if (!byte.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out byte byteresult))
					break;

				result = (int) byteresult;
				return true;

			case MetadataType.SByte:
				if (!sbyte.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out sbyte sbyteresult))
					break;

				result = (int) sbyteresult;
				return true;

			case MetadataType.Int16:
				if (!short.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out short shortresult))
					break;

				result = (int) shortresult;
				return true;

			case MetadataType.UInt16:
				if (!ushort.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ushort ushortresult))
					break;

				result = (int) ushortresult;
				return true;

			case MetadataType.Int32:
				if (!int.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iresult))
					break;

				result = iresult;
				return true;

			case MetadataType.UInt32:
				if (!uint.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out uint uresult))
					break;

				result = (int) uresult;
				return true;

			case MetadataType.Double:
				if (!double.TryParse (value, NumberStyles.Float, CultureInfo.InvariantCulture, out double dresult))
					break;

				result = dresult;
				return true;

			case MetadataType.Single:
				if (!float.TryParse (value, NumberStyles.Float, CultureInfo.InvariantCulture, out float fresult))
					break;

				result = fresult;
				return true;

			case MetadataType.Int64:
				if (!long.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long lresult))
					break;

				result = lresult;
				return true;

			case MetadataType.UInt64:
				if (!ulong.TryParse (value, NumberStyles.Integer, CultureInfo.InvariantCulture, out ulong ulresult))
					break;

				result = (long) ulresult;
				return true;

			case MetadataType.Char:
				if (!char.TryParse (value, out char chresult))
					break;

				result = (int) chresult;
				return true;

			case MetadataType.String:
				if (value is string || value == null) {
					result = value;
					return true;
				}

				break;
			}

			result = null;
			return false;
		}

		static MethodDefinition FindMethod (TypeDefinition type, string signature)
		{
			if (!type.HasMethods)
				return null;

			foreach (MethodDefinition meth in type.Methods)
				if (signature == ResolveFromXmlStep.GetMethodSignature (meth, includeGenericParameters: true))
					return meth;

			return null;
		}

		static AssemblyNameReference GetAssemblyName (XPathNavigator nav)
		{
			return AssemblyNameReference.Parse (GetAttribute (nav, "fullname"));
		}

		static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, "");
		}
	}
}
