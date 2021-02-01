using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Xml.XPath;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : ProcessLinkerXmlStepBase
	{
		public BodySubstituterStep (XPathDocument document, string xmlDocumentLocation)
			: base (document, xmlDocumentLocation)
		{
		}

		public BodySubstituterStep (XPathDocument document, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "")
			: base (document, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		protected override void Process ()
		{
			ProcessXml (Context.StripSubstitutions, Context.IgnoreSubstitutions);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
			ProcessResources (assembly, nav.SelectChildren ("resource", ""));
		}

		protected override TypeDefinition ProcessExportedType (ExportedType exported, AssemblyDefinition assembly) => null;

		protected override bool ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav) => false;

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));

			if (!nav.HasChildren)
				return;

			XPathNodeIterator methods = nav.SelectChildren ("method", "");
			if (methods.Count > 0)
				ProcessMethods (type, methods);

			var fields = nav.SelectChildren ("field", "");
			if (fields.Count > 0) {
				while (fields.MoveNext ()) {
					if (!ShouldProcessElement (fields.Current))
						continue;

					ProcessField (type, fields);
				}
			}
		}

		void ProcessMethods (TypeDefinition type, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				if (!ShouldProcessElement (iterator.Current))
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
				Context.LogWarning ($"Could not find method '{signature}' on type '{type.GetDisplayName ()}'", 2009, _xmlDocumentLocation);
				return;
			}

			string action = GetAttribute (iterator.Current, "body");
			switch (action) {
			case "remove":
				Annotations.SetAction (method, MethodAction.ConvertToThrow);
				return;
			case "stub":
				string value = GetAttribute (iterator.Current, "value");
				if (!string.IsNullOrEmpty (value)) {
					if (!TryConvertValue (value, method.ReturnType, out object res)) {
						Context.LogWarning ($"Invalid value for '{method.GetDisplayName ()}' stub", 2010, _xmlDocumentLocation);
						return;
					}

					Annotations.SetMethodStubValue (method, res);
				}

				Annotations.SetAction (method, MethodAction.ConvertToStub);
				return;
			default:
				Context.LogWarning ($"Unknown body modification '{action}' for '{method.GetDisplayName ()}'", 2011, _xmlDocumentLocation);
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
				Context.LogWarning ($"Could not find field '{name}' on type '{type.GetDisplayName ()}'", 2012, _xmlDocumentLocation);
				return;
			}

			if (!field.IsStatic || field.IsLiteral) {
				Context.LogWarning ($"Substituted field '{field.GetDisplayName ()}' needs to be static field.", 2013, _xmlDocumentLocation);
				return;
			}

			string value = GetAttribute (iterator.Current, "value");
			if (string.IsNullOrEmpty (value)) {
				Context.LogWarning ($"Missing 'value' attribute for field '{field.GetDisplayName ()}'.", 2014, _xmlDocumentLocation);
				return;
			}
			if (!TryConvertValue (value, field.FieldType, out object res)) {
				Context.LogWarning ($"Invalid value '{value}' for '{field.GetDisplayName ()}'.", 2015, _xmlDocumentLocation);
				return;
			}

			Annotations.SetFieldValue (field, res);

			string init = GetAttribute (iterator.Current, "initialize");
			if (init?.ToLowerInvariant () == "true") {
				Annotations.SetSubstitutedInit (field);
			}
		}

		void ProcessResources (AssemblyDefinition assembly, XPathNodeIterator iterator)
		{
			while (iterator.MoveNext ()) {
				XPathNavigator nav = iterator.Current;

				if (!ShouldProcessElement (nav))
					continue;

				string name = GetAttribute (nav, "name");
				if (String.IsNullOrEmpty (name)) {
					Context.LogWarning ($"Missing 'name' attribute for resource.", 2038, _xmlDocumentLocation);
					continue;
				}

				string action = GetAttribute (nav, "action");
				if (action != "remove") {
					Context.LogWarning ($"Invalid value '{action}' for attribute 'action' for resource '{name}'.", 2039, _xmlDocumentLocation);
					continue;
				}

				EmbeddedResource resource = assembly.FindEmbeddedResource (name);
				if (resource == null) {
					Context.LogWarning ($"Could not find embedded resource '{name}' to remove in assembly '{assembly.Name.Name}'.", 2040, _xmlDocumentLocation);
					continue;
				}

				Context.Annotations.AddResourceToRemove (assembly, resource);
			}
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

		protected override AssemblyDefinition GetAssembly (LinkContext context, AssemblyNameReference assemblyName)
		{
			return context.TryResolve (assemblyName.Name);
		}
	}
}
