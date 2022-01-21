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
				var attributes = ProcessAttributes (nav, AttributeTargets.Assembly);
				var types = ProcessTypes (nav);
				return new LinkAttributesAssembly (GetFullName (nav), attributes: attributes) {
					Types = types,
					FeatureGate = ProcessFeatureGate (nav),
				};
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
				
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
						var methods = ProcessMethod (methodNav, childContext);
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
						var properties = ProcessMember (propertyNav, childContext, AttributeTargets.Property);
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
						var events = ProcessMember (eventNav, childContext, AttributeTargets.Event);
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
						var fields = ProcessMember (fieldNav, childContext, AttributeTargets.Field);
				}

				var nestedTypes = ProcessTypes (nav);
				var attributes = ProcessAttributes (nav);
				if (isNested) {
					var name = GetName (nav);
					return new LinkAttributesNestedType (name: name, attributes: attributes, methods: methods, properties: properties, fields: fields, events:events, nestedTypes:nestedTypes);
				} else {
					var fullname = GetFullName (nav);
					return new LinkAttributesNestedType (fullname: fullname, attributes: attributes, methods: methods, properties: properties, fields: fields, events:events, nestedTypes:nestedTypes);
				}
				
			}

			LinkAttributesTypeMember ProcessMember (XPathNavigator nav)
			{
				var attributes = ProcessAttributes (nav);
				return new LinkAttributesTypeMember (attributes: attributes, name:GetName(nav), signature: GetSignature(nav));
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

			LinkAttributesAttribute? ProcessAttribute (XPathNavigator nav)
			{
				var arguments = new List<LinkAttributesAttributeArgument> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (argNav, "type");
					if (type == "") type = "System.String";
					var arg = new LinkAttributesAttributeArgument (type, argNav.Value);
					arguments.Add (arg);
				}
				var properties = new List<LinkAttributesAttributeProperty> ();
				foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (propertyNav, "type");
					if (type == "") type = "System.String";
					var prop = new LinkAttributesAttributeProperty (name: GetName (propertyNav), type: type, value: propertyNav.Value);
					properties.Add (prop);
				}
				var fields = new List<LinkAttributesAttributeField> ();
				foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (fieldNav, "type");
					if (type == "") type = "System.String";
					var field = new LinkAttributesAttributeField (name: GetName (fieldNav), type: type, value: fieldNav.Value);
					fields.Add (field);
				}
				return new LinkAttributesAttribute (fullname: GetFullName(nav), @internal: GetAttribute(nav, "internal"), assembly: GetAttribute(nav, AssemblyElementName), );
			}
		}
	}

	public record LinkAttributesAttribute
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

		public LinkAttributesAttributeField (string name, string type, string value)
		{
			Value = value;
			Name = name;
		}
	}

	public partial record LinkAttributesTypeMember : LinkAttributesFeatureGated, ILinkAttributesNode
	{
		public string? Name;
		public string? Signature;
		public LinkAttributesTypeMember(List<LinkAttributesAttribute> attributes) : base (attributes)
		{
		}
		public LinkAttributesTypeMember(List<LinkAttributesAttribute> attributes, string? name, string? signature) : base (attributes)
		{
			Name = name;
			Signature = signature;
		}
	}

	public partial record LinkAttributesParameter : ILinkAttributesNode
	{
		public string Name;
		public List<LinkAttributesAttribute>? Attributes { get; }
		public LinkAttributesParameter(string name)
		{
			Name = name;
		}
	}

	public partial record LinkAttributesMethod : LinkAttributesTypeMember, ILinkAttributesNode
	{
		public List<LinkAttributesParameter>? Parameters;
		public List<LinkAttributesAttribute>? ReturnAttributes;
		public LinkAttributesMethod(string? name, string? signature, List<LinkAttributesParameter> parameters, List<LinkAttributesAttribute> attributes, List<LinkAttributesAttribute>? returnAttributes, LinkAttributesFeatureGate featureGate) : base (attributes, name:name, signature:signature) 
		{
			Parameters = parameters;
			ReturnAttributes = returnAttributes;
			FeatureGate = featureGate;
		}
	}

	public partial record LinkAttributesNestedType : LinkAttributesFeatureGated, ILinkAttributesNode
	{
		public string Name;
		public List<LinkAttributesTypeMember>? Events;
		public List<LinkAttributesTypeMember>? Fields;
		public List<LinkAttributesTypeMember>? Properties;
		public List<LinkAttributesMethod>? Methods;
		public List<LinkAttributesNestedType>? Types;
		public LinkAttributesNestedType (string name, List<LinkAttributesAttribute> attributes, List<LinkAttributesMethod>? methods, List<LinkAttributesTypeMember>? properties, List<LinkAttributesTypeMember>? fields, List<LinkAttributesTypeMember>? events, List<LinkAttributesNestedType> nestedTypes) : base(attributes)
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
		public List<LinkAttributesMethod>? Methods;
		public List<LinkAttributesTypeMember>? Properties;
		public List<LinkAttributesTypeMember>? Fields;
		public List<LinkAttributesTypeMember>? Events;
		public List<LinkAttributesNestedType>? Types;
		public LinkAttributesType (string fullname, List<LinkAttributesAttribute> attributes, List<LinkAttributesMethod>? methods, List<LinkAttributesTypeMember>? properties, List<LinkAttributesTypeMember>? fields, List<LinkAttributesTypeMember>? events, List<LinkAttributesNestedType> nestedTypes) : base(attributes)
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
		public LinkAttributesAssembly (string fullname, List<LinkAttributesAttribute> attributes, List<LinkAttributesType>? types) : base (attributes)
		{
			Fullname = fullname;
			Types = types;
		}
	}

	public interface ILinkAttributesRootNode  : ILinkAttributesNode { }

	public interface ILinkAttributesNode
	{
		public List<LinkAttributesAttribute>? Attributes { get; }
	}

	public abstract record LinkAttributesFeatureGated : ILinkAttributesNode
	{
		public List<LinkAttributesAttribute>? Attributes { get; }
		public LinkAttributesFeatureGate? FeatureGate;
		public LinkAttributesFeatureGated (List<LinkAttributesAttribute>? attributes)
		{
			Attributes = attributes;
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