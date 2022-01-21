// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Xml.XPath;
using System.Reflection.Metadata;
using System.Xml.Schema;
using System.Collections.Generic;
using System.Diagnostics;
using ILLink.Shared;
using System.Collections;
using System.Net.NetworkInformation;
using System.Data;
using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics.Contracts;
using static ILLink.RoslynAnalyzer.LinkAttributesTypeBase;

namespace ILLink.RoslynAnalyzer
{
	class AnalyzerXmlAttributeParser : XmlProcessorBase
	{
		public AnalyzerXmlAttributeParser (string xmlDocumentLocation, Stream documentStream) : base (xmlDocumentLocation, documentStream) { }

		private static Stream? GenerateStream (string xmlDocumentLocation, CompilationStartAnalysisContext context)
		{
			ImmutableArray<AdditionalText> additionalFiles = context.Options.AdditionalFiles;
			AdditionalText? xmlFile = additionalFiles.FirstOrDefault (file => Path.GetFileName (file.Path).Contains (xmlDocumentLocation));
			if (xmlFile == null) {
				return null;
			}
			SourceText? fileText = xmlFile.GetText (context.CancellationToken);
			if (fileText == null) {
				throw new NotImplementedException ();
			}
			MemoryStream stream = new MemoryStream ();
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				fileText.Write (writer);
			}

			stream.Position = 0;
			return stream;
		}

		static bool ShouldProcessAllAssemblies (XPathNavigator nav) => GetFullName (nav) == AllAssembliesFullName;

		// Looks at features settings in the XML to determine whether the element should be processed
		// Assumes defaults if present, otherwise assumes features are not featurevalue
		// i.e. we don't process an element if there is a feature gate unless the default is the featurevalue
		static LinkAttributesFeatureGate? ProcessFeatureGate (XPathNavigator nav)
		{
			string? feature = GetAttribute (nav, "feature");
			if (String.IsNullOrEmpty (feature)) 
				feature = null;
			string? featurevalue = GetAttribute (nav, "featurevalue");
			if (String.IsNullOrEmpty (featurevalue))
				featurevalue = null;
			string? featuredefault = GetAttribute (nav, "featuredefault");
			if (String.IsNullOrEmpty (featuredefault))
				featuredefault = null;
			if (feature is null && featurevalue is null && featuredefault is null) {
				return null;
			}
			return new LinkAttributesFeatureGate (feature, featurevalue, featuredefault);
			
		}

		public static List<ILinkAttributesRootNode> ProcessXml (CompilationStartAnalysisContext compilationContext)
		{
			var injectionContext = new LinkAttributesContext ();
			var roots = new List<ILinkAttributesRootNode> ();
			Stream? stream = GenerateStream ("ILLink.LinkAttributes.xml", compilationContext);
			if (stream == null)
				return roots;
			XPathDocument doc = new XPathDocument (stream);
			try {
				XPathNavigator nav = doc.CreateNavigator ();


				if (!nav.MoveToChild (LinkerElementName, XmlNamespace))
					return roots;

				if (nav.SchemaInfo.Validity == XmlSchemaValidity.Invalid) { }
				// Warn about invalid xml according to schema

				roots.Concat (ProcessAssemblies (nav));
				roots.Concat (ProcessTypes (nav));
			}
			// TODO: handle the correct exceptions correctly
			catch (Exception) {
				throw new NotImplementedException ("Processing XML failed and was not handled");
			}
			return roots;

			List<LinkAttributesAssembly> ProcessAssemblies (XPathNavigator nav)
			{
				var assemblies = new List<LinkAttributesAssembly> ();
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					// Errors for invalid assembly names should show up even if this element will be
					// skipped due to feature conditions.
					bool processAllAssemblies = ShouldProcessAllAssemblies (assemblyNav);

					if (ProcessAssembly (assemblyNav) is LinkAttributesAssembly assembly)
						assemblies.Add(assembly);
				}
				return assemblies;
			}

			LinkAttributesAssembly? ProcessAssembly (XPathNavigator nav)
			{
				var attributes = ProcessAttributes (nav);
				var types = ProcessTypes (nav);
				return new LinkAttributesAssembly (fullname: GetFullName (nav), attributes: attributes, types: types, featureGate: ProcessFeatureGate (nav));
			}

			/// <summary>
			///		Takes an XPathNavigator that might have <type> children and processes those children
			/// </summary>
			List<LinkAttributesType> ProcessTypes (XPathNavigator nav)
			{
				var types = new List<LinkAttributesType> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					ProcessType (typeNav, false);
				}
				return types;
			}

			LinkAttributesType ProcessType (XPathNavigator nav, bool isNested)
			{
				var methods = new List<LinkAttributesMethod> ();
				var properties = new List<LinkAttributesTypeMember> ();
				var fields = new List<LinkAttributesTypeMember> ();
				var events = new List<LinkAttributesTypeMember> ();
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
						methods.Add(ProcessMethod (methodNav));
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
						properties.Add(ProcessMember (propertyNav));
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
						events.Add(ProcessMember (eventNav));
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
						fields.Add(ProcessMember (fieldNav));
				}

				var nestedTypes = ProcessTypes (nav);
				var attributes = ProcessAttributes (nav);
				if (isNested) {
					var name = GetName (nav);
					return new LinkAttributesNestedType (name: name, attributes: attributes, methods: methods, properties: properties, fields: fields, events:events, nestedTypes:nestedTypes, ProcessFeatureGate(nav));
				} else {
					var fullname = GetFullName (nav);
					return new LinkAttributesType (fullname: fullname, attributes: attributes, methods: methods, properties: properties, fields: fields, events:events, nestedTypes:nestedTypes, featureGate: ProcessFeatureGate(nav));
				}
				
			}

			LinkAttributesTypeMember ProcessMember (XPathNavigator nav)
			{
				var attributes = ProcessAttributes (nav);
				return new LinkAttributesTypeMember (attributes: attributes, name:GetName(nav), signature: GetSignature(nav), ProcessFeatureGate(nav));
			}

			LinkAttributesParameter ProcessParameter(XPathNavigator nav)
			{
				return new LinkAttributesParameter (name: GetName (nav), attributes: ProcessAttributes (nav));
			}

			LinkAttributesMethod ProcessMethod (XPathNavigator nav)
			{
				var attributes = ProcessAttributes (nav);
				var parameters = new List<LinkAttributesParameter> ();
				var returnAttributes = new List<LinkAttributesAttribute> ();
				foreach (XPathNavigator parameterNav in nav.SelectChildren (ParameterElementName, "")) {
					parameters.Add (ProcessParameter (parameterNav));
				}
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					returnAttributes.Concat(ProcessAttributes (nav));
				}
				return new LinkAttributesMethod (name: GetName (nav), signature: GetSignature (nav), parameters: parameters, attributes: attributes, returnAttributes: returnAttributes, featureGate: ProcessFeatureGate (nav));
			}

			List<LinkAttributesAttribute> ProcessAttributes (XPathNavigator nav)
			{
				var attributes = new List<LinkAttributesAttribute> ();
				foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
					var attr = ProcessAttribute (attributeNav);
					if (attr == null)
						continue;
					attributes.Add (attr);
				}
				return attributes;
			}

			ILinkAttributesAttributeArgument? ProcessArgument (XPathNavigator argNav)
			{
				var type = GetAttribute (argNav, "type");
				if (type == "") type = "System.String";
				if (type == "System.Object") {
					foreach(XPathNavigator innerArgNav in argNav.SelectChildren(ArgumentElementName, "")) {
						if (ProcessArgument(innerArgNav) is LinkAttributesAttributeArgument innerArg) {
							return new LinkAttributesAttributeArgumentBox (innerArg);
						}
						else {
							// error
							return null;
						}
					}
				}
				var arg = new LinkAttributesAttributeArgument (type, argNav.Value);
				return arg;
			}

			LinkAttributesAttribute? ProcessAttribute (XPathNavigator nav)
			{
				var arguments = new List<ILinkAttributesAttributeArgument> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					if (ProcessArgument (argNav) is LinkAttributesAttributeArgument arg) {
						arguments.Add (arg);
					}
				}
				var properties = new List<LinkAttributesAttributeProperty> ();
				foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (propertyNav, "type");
					if (type == "") type = "System.String";
					var prop = new LinkAttributesAttributeProperty (name: GetName (propertyNav), value: propertyNav.Value);
					properties.Add (prop);
				}
				var fields = new List<LinkAttributesAttributeField> ();
				foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (fieldNav, "type");
					if (type == "") type = "System.String";
					var field = new LinkAttributesAttributeField (name: GetName (fieldNav), type: type, value: fieldNav.Value);
					fields.Add (field);
				}
				return new LinkAttributesAttribute (fullname: GetFullName(nav), @internal: GetAttribute(nav, "internal"), assembly: GetAttribute(nav, AssemblyElementName), arguments: arguments, properties: properties, fields: fields);
			}
		}
	}

	public record LinkAttributesAttribute : LinkAttributesNode
	{
		public string Fullname;
		public string? Internal;
		public string? Assembly;
		public List<ILinkAttributesAttributeArgument>? Arguments;
		public List<LinkAttributesAttributeProperty>? Properties;
		public List<LinkAttributesAttributeField>? Fields;

		public LinkAttributesAttribute (string fullname, string? @internal, string? assembly, List<ILinkAttributesAttributeArgument>? arguments, List<LinkAttributesAttributeProperty>? properties, List<LinkAttributesAttributeField>? fields)
		{
			Fullname = fullname;
			Internal = @internal;
			Assembly = assembly;
			Arguments = arguments;
			Properties = properties;
			Fields = fields;
		}

		public LinkAttributesAttribute (XPathNavigator nav)
		{
			var arguments = new List<ILinkAttributesAttributeArgument> ();
			foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
				if (ProcessArgument (argNav) is LinkAttributesAttributeArgument arg) {
					arguments.Add (arg);
				}
			}
			var properties = new List<LinkAttributesAttributeProperty> ();
			foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
				var prop = new LinkAttributesAttributeProperty (name: GetName (propertyNav), value: propertyNav.Value);
				properties.Add (prop);
			}
			var fields = new List<LinkAttributesAttributeField> ();
			foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
				var field = new LinkAttributesAttributeField (name: GetName (fieldNav), value: fieldNav.Value);
				fields.Add (field);
			}
			Fullname= GetFullName(nav);
			Internal= GetAttribute(nav, "internal");
			Assembly= GetAttribute(nav, AssemblyElementName);
			Arguments= arguments;
			Properties= properties;
			Fields= fields;

			ILinkAttributesAttributeArgument? ProcessArgument (XPathNavigator argNav)
			{
			var type = argNav.GetAttribute ("type", "");
			if (type == "") type = "System.String";
			if (type == "System.Object") {
				foreach(XPathNavigator innerArgNav in argNav.SelectChildren(ArgumentElementName, "")) {
					if (ProcessArgument(innerArgNav) is LinkAttributesAttributeArgument innerArg) {
						return new LinkAttributesAttributeArgumentBox (innerArg);
					}
					else {
						// error
						return null;
					}
				}
			}
			var arg = new LinkAttributesAttributeArgument (type, argNav.Value);
			return arg;
			}
		}
	}

	public interface ILinkAttributesAttributeArgument { }

	public record LinkAttributesAttributeArgumentBox : ILinkAttributesAttributeArgument
	{
		public Type Type = typeof(System.Object);
		public LinkAttributesAttributeArgument InnerArgument;

		public LinkAttributesAttributeArgumentBox (LinkAttributesAttributeArgument innerArgument)
		{
			InnerArgument = innerArgument;
		}
	}

	public record LinkAttributesAttributeArgument : ILinkAttributesAttributeArgument
	{
		public string Type;
		public string Value;

		public LinkAttributesAttributeArgument (string type, string value)
		{
			Type = type;
			Value = value;
		}
	}

	public record LinkAttributesAttributeProperty
	{
		public string Name;
		public string Value;

		public LinkAttributesAttributeProperty (string name, string value)
		{
			Value = value;
			Name = name;
		}
	}

	public record LinkAttributesAttributeField
	{
		public string Name;
		public string Value;

		public LinkAttributesAttributeField (string name, string value)
		{
			Value = value;
			Name = name;
		}
	}

	public partial record LinkAttributesTypeMember : LinkAttributesFeatureGated 
	{
		public string? Name;
		public string? Signature;
		public LinkAttributesTypeMember(List<LinkAttributesAttribute> attributes, string? name, string? signature, LinkAttributesFeatureGate? featureGate) : base (attributes, featureGate)
		{
			Name = name;
			Signature = signature;
		}
		public LinkAttributesTypeMember (XPathNavigator nav) : base (nav)
		{
			Name = GetName (nav);
			Signature = GetSignature (nav);
		}
	}

	public partial record LinkAttributesParameter 
	{
		public string Name;
		public List<LinkAttributesAttribute>? Attributes { get; }
		public LinkAttributesParameter(string name, List<LinkAttributesAttribute> attributes)
		{
			Name = name;
			Attributes = attributes;
		}
	}

	public partial record LinkAttributesMethod : LinkAttributesTypeMember
	{
		public List<LinkAttributesParameter>? Parameters;
		public List<LinkAttributesAttribute>? ReturnAttributes;
		public LinkAttributesMethod(string? name, string? signature, List<LinkAttributesParameter> parameters, List<LinkAttributesAttribute> attributes, List<LinkAttributesAttribute>? returnAttributes, LinkAttributesFeatureGate? featureGate) : base (attributes, name:name, signature:signature, featureGate) 
		{
			Parameters = parameters;
			ReturnAttributes = returnAttributes;
		}
	}

	public abstract record LinkAttributesTypeBase : LinkAttributesFeatureGated
	{
		public List<LinkAttributesTypeMember>? Events;
		public List<LinkAttributesTypeMember>? Fields;
		public List<LinkAttributesTypeMember>? Properties;
		public List<LinkAttributesMethod>? Methods;
		public List<LinkAttributesNestedType>? Types;
		public LinkAttributesTypeBase(XPathNavigator nav) : base (nav)
		{
			Methods = new List<LinkAttributesMethod> ();
			Properties = new List<LinkAttributesTypeMember> ();
			Fields = new List<LinkAttributesTypeMember> ();
			Events = new List<LinkAttributesTypeMember> ();
			foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
					Methods.Add(new LinkAttributesMethod (methodNav));
			}
			foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
					Properties.Add(new LinkAttributesTypeMember (propertyNav));
			}
			foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
					Events.Add(new LinkAttributesTypeMember (eventNav));
			}
			foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
					Fields.Add(new LinkAttributesTypeMember (fieldNav));
			}
			Types = new List<LinkAttributesNestedType> ();
			foreach (XPathNavigator nestedTypeNav in nav.SelectChildren (TypeElementName, "")) {
				Types.Add(new LinkAttributesNestedType(nestedTypeNav));
			}
		}
	}
	public partial record LinkAttributesNestedType : LinkAttributesFeatureGated
	{
		public string Name;
		public LinkAttributesNestedType (string name, List<LinkAttributesAttribute> attributes, List<LinkAttributesMethod>? methods, List<LinkAttributesTypeMember>? properties, List<LinkAttributesTypeMember>? fields, List<LinkAttributesTypeMember>? events, List<LinkAttributesNestedType> nestedTypes, LinkAttributesFeatureGate? featureGate) : base(attributes, featureGate)
		{
			Name = name;
			Methods = methods;
			Properties = properties;
			Fields = fields;
			Events = events;
			Types = nestedTypes;
		}
	}

	public partial record LinkAttributesType : LinkAttributesFeatureGated, ILinkAttributesRootNode
	{
		public string Fullname;
		public LinkAttributesType (string fullname, List<LinkAttributesAttribute> attributes, List<LinkAttributesMethod>? methods, List<LinkAttributesTypeMember>? properties, List<LinkAttributesTypeMember>? fields, List<LinkAttributesTypeMember>? events, List<LinkAttributesNestedType> nestedTypes, LinkAttributesFeatureGate? featureGate) : base(attributes, featureGate)
		{
			Fullname = fullname;
			Methods = methods;
			Properties = properties;
			Fields = fields;
			Events = events;
			Types = nestedTypes;
		}
	}

	public record LinkAttributesAssembly : LinkAttributesFeatureGated, ILinkAttributesRootNode
	{
		public List<LinkAttributesType>? Types;
		public string Fullname;
		public LinkAttributesAssembly (string fullname, List<LinkAttributesAttribute> attributes, List<LinkAttributesType>? types, LinkAttributesFeatureGate? featureGate) : base (attributes, featureGate)
		{
			Fullname = fullname;
			Types = types;
		}
	}

	public interface ILinkAttributesRootNode { }

	public abstract record LinkAttributesNode
	{	protected const string FullNameAttributeName = "fullname";
		protected const string LinkerElementName = "linker";
		protected const string TypeElementName = "type";
		protected const string SignatureAttributeName = "signature";
		protected const string NameAttributeName = "name";
		protected const string FieldElementName = "field";
		protected const string MethodElementName = "method";
		protected const string EventElementName = "event";
		protected const string PropertyElementName = "property";
		protected const string AttributeElementName = "attribute";
		protected const string AssemblyElementName = "assembly";
		protected const string ArgumentElementName = "argument";
		protected const string ParameterElementName = "parameter";
		protected const string ReturnElementName = "return";
		protected const string AllAssembliesFullName = "*";
		protected const string XmlNamespace = "";


		protected static string GetFullName (XPathNavigator nav)
		{
			return GetAttribute (nav, FullNameAttributeName);
		}

		protected static string GetName (XPathNavigator nav)
		{
			var name = GetAttribute (nav, NameAttributeName);
			return name;
		}

		protected static string GetSignature (XPathNavigator nav)
		{
			return GetAttribute (nav, SignatureAttributeName);
		}

		protected static string GetAttribute (XPathNavigator nav, string attribute)
		{
			return nav.GetAttribute (attribute, XmlNamespace);
		}

	}
	public abstract record LinkAttributesAttributeTarget : LinkAttributesNode
	{

		public List<LinkAttributesAttribute> Attributes;

		public LinkAttributesAttributeTarget (List<LinkAttributesAttribute> attributes)
		{
			Attributes = attributes;
		}

		public LinkAttributesAttributeTarget(XPathNavigator nav)
		{
			var attributes = new List<LinkAttributesAttribute> ();
			foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
				var attr = new LinkAttributesAttribute (attributeNav);
				if (attr == null)
					continue;
				attributes.Add (attr);
			}
			Attributes = attributes;
		}
	}

	public abstract record LinkAttributesFeatureGated : LinkAttributesAttributeTarget
	{
		public LinkAttributesFeatureGate? FeatureGate;
		public LinkAttributesFeatureGated (List<LinkAttributesAttribute> attributes, LinkAttributesFeatureGate? featureGate) : base (attributes)
		{
			FeatureGate = featureGate;
		}
		public LinkAttributesFeatureGated (XPathNavigator nav) : base(nav)
		{
			string? feature = GetAttribute (nav, "feature");
			if (String.IsNullOrEmpty (feature)) 
				feature = null;
			string? featurevalue = GetAttribute (nav, "featurevalue");
			if (String.IsNullOrEmpty (featurevalue))
				featurevalue = null;
			string? featuredefault = GetAttribute (nav, "featuredefault");
			if (String.IsNullOrEmpty (featuredefault))
				featuredefault = null;
			if (feature is null && featurevalue is null && featuredefault is null) {
				return;
			}
			FeatureGate = new LinkAttributesFeatureGate (feature, featurevalue, featuredefault);
		}
	}

	public record LinkAttributesFeatureGate
	{
		public string? Feature;
		public string? FeatureValue;
		public string? FeatureDefault;
		public LinkAttributesFeatureGate (string? feature, string? featureValue, string? featureDefault)
		{
			Feature = feature;
			FeatureValue = featureValue;
			FeatureDefault = featureDefault;
		}
	}


	internal struct LinkAttributesContext
	{
		public string Assembly;
		public string Path;

		internal LinkAttributesContext (string assembly, string path)
		{
			Path = path;
			Assembly = assembly;
		}
		public LinkAttributesContext ()
		{
			Path = "";
			Assembly = "";
		}
	}
}