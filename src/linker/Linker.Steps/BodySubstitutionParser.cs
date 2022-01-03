using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class BodySubstitutionParser : ProcessLinkerXmlBase
	{
		SubstitutionInfo? _substitutionInfo;

		public BodySubstitutionParser (LinkContext context, Stream documentStream, string xmlDocumentLocation)
			: base (context, documentStream, xmlDocumentLocation)
		{
		}

		public BodySubstitutionParser (LinkContext context, Stream documentStream, EmbeddedResource resource, AssemblyDefinition resourceAssembly, string xmlDocumentLocation = "")
			: base (context, documentStream, resource, resourceAssembly, xmlDocumentLocation)
		{
		}

		public void Parse (SubstitutionInfo xmlInfo)
		{
			_substitutionInfo = xmlInfo;
			bool stripSubstitutions = _context.IsOptimizationEnabled (CodeOptimizations.RemoveSubstitutions, _resource?.Assembly);
			ProcessXml (stripSubstitutions, _context.IgnoreSubstitutions);
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly, XPathNavigator nav, bool warnOnUnresolvedTypes)
		{
			ProcessTypes (assembly, nav, warnOnUnresolvedTypes);
			ProcessResources (assembly, nav);
		}

		protected override TypeDefinition? ProcessExportedType (ExportedType exported, AssemblyDefinition assembly, XPathNavigator nav) => null;

		protected override bool ProcessTypePattern (string fullname, AssemblyDefinition assembly, XPathNavigator nav) => false;

		protected override void ProcessType (TypeDefinition type, XPathNavigator nav)
		{
			Debug.Assert (ShouldProcessElement (nav));
			ProcessTypeChildren (type, nav);
		}

		protected override void ProcessMethod (TypeDefinition type, XPathNavigator methodNav, object? _customData)
		{
			Debug.Assert (_substitutionInfo != null);
			string signature = GetSignature (methodNav);
			if (string.IsNullOrEmpty (signature))
				return;

			MethodDefinition? method = FindMethod (type, signature);
			if (method == null) {
				LogWarning (new DiagnosticString (DiagnosticId.XmlCouldNotFindMethodOnType).GetMessage (signature, type.GetDisplayName ()), DiagnosticId.XmlCouldNotFindMethodOnType, methodNav);
				return;
			}

			string action = GetAttribute (methodNav, "body");
			switch (action) {
			case "remove":
				_substitutionInfo.SetMethodAction (method, MethodAction.ConvertToThrow);
				return;
			case "stub":
				string value = GetAttribute (methodNav, "value");
				if (!string.IsNullOrEmpty (value)) {
					if (!TryConvertValue (value, method.ReturnType, out object? res)) {
						LogWarning (new DiagnosticString (DiagnosticId.XmlInvalidValueForStub).GetMessage (method.GetDisplayName ()), DiagnosticId.XmlInvalidValueForStub, methodNav);
						return;
					}

					_substitutionInfo.SetMethodStubValue (method, res);
				}

				_substitutionInfo.SetMethodAction (method, MethodAction.ConvertToStub);
				return;
			default:
				LogWarning (new DiagnosticString (DiagnosticId.XmlUnkownBodyModification).GetMessage (action, method.GetDisplayName ()), DiagnosticId.XmlUnkownBodyModification, methodNav);
				return;
			}
		}

		protected override void ProcessField (TypeDefinition type, XPathNavigator fieldNav)
		{
			Debug.Assert (_substitutionInfo != null);
			string name = GetAttribute (fieldNav, "name");
			if (string.IsNullOrEmpty (name))
				return;

			var field = type.Fields.FirstOrDefault (f => f.Name == name);
			if (field == null) {
				LogWarning (new DiagnosticString (DiagnosticId.XmlCouldNotFindFieldOnType).GetMessage (name, type.GetDisplayName ()), DiagnosticId.XmlCouldNotFindFieldOnType, fieldNav);
				return;
			}

			if (!field.IsStatic || field.IsLiteral) {
				LogWarning (new DiagnosticString (DiagnosticId.XmlSubstitutedFieldNeedsToBeStatic).GetMessage (field.GetDisplayName ()), DiagnosticId.XmlSubstitutedFieldNeedsToBeStatic, fieldNav);
				return;
			}

			string value = GetAttribute (fieldNav, "value");
			if (string.IsNullOrEmpty (value)) {
				LogWarning (new DiagnosticString (DiagnosticId.XmlMissingSubstitutionValueForField).GetMessage (field.GetDisplayName ()), DiagnosticId.XmlMissingSubstitutionValueForField, fieldNav);
				return;
			}
			if (!TryConvertValue (value, field.FieldType, out object? res)) {
				LogWarning (new DiagnosticString (DiagnosticId.XmlInvalidSubstitutionValueForField).GetMessage (value, field.GetDisplayName ()), DiagnosticId.XmlInvalidSubstitutionValueForField, fieldNav);
				return;
			}

			_substitutionInfo.SetFieldValue (field, res);

			string init = GetAttribute (fieldNav, "initialize");
			if (init?.ToLowerInvariant () == "true") {
				_substitutionInfo.SetFieldInit (field);
			}
		}

		void ProcessResources (AssemblyDefinition assembly, XPathNavigator nav)
		{
			foreach (XPathNavigator resourceNav in nav.SelectChildren ("resource", "")) {
				if (!ShouldProcessElement (resourceNav))
					continue;

				string name = GetAttribute (resourceNav, "name");
				if (String.IsNullOrEmpty (name)) {
					LogWarning (new DiagnosticString (DiagnosticId.XmlMissingNameAttributeInResource).GetMessage (), DiagnosticId.XmlMissingNameAttributeInResource, resourceNav);
					continue;
				}

				string action = GetAttribute (resourceNav, "action");
				if (action != "remove") {
					LogWarning (new DiagnosticString (DiagnosticId.XmlInvalidValueForAttributeActionForResource).GetMessage (action, name),
						DiagnosticId.XmlInvalidValueForAttributeActionForResource, resourceNav);
					continue;
				}

				EmbeddedResource? resource = assembly.FindEmbeddedResource (name);
				if (resource == null) {
					LogWarning (new DiagnosticString (DiagnosticId.XmlCouldNotFindResourceToRemoveInAssembly).GetMessage (name, assembly.Name.Name),
						DiagnosticId.XmlCouldNotFindResourceToRemoveInAssembly, resourceNav);
					continue;
				}

				_context.Annotations.AddResourceToRemove (assembly, resource);
			}
		}

		static MethodDefinition? FindMethod (TypeDefinition type, string signature)
		{
			if (!type.HasMethods)
				return null;

			foreach (MethodDefinition meth in type.Methods)
				if (signature == DescriptorMarker.GetMethodSignature (meth, includeGenericParameters: true))
					return meth;

			return null;
		}
	}
}
