// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ILLink.Shared
{
	public static class LinkAttributes
	{
		public static LinkerNode ProcessXml (XDocument doc)
		{
			XPathNavigator nav = doc.CreateNavigator ();
			return new LinkerNode (nav);
		}

		public record AttributeNode : NodeBase
		{
			public string FullName;
			public string Internal;
			public string Assembly;
			public List<AttributeArgumentNodeBase> Arguments;
			public List<AttributePropertyNode> Properties;
			public List<AttributeFieldNode> Fields;

			public AttributeNode (XPathNavigator nav) : base (nav)
			{
				var diagnostics = new List<ParsingDiagnostic> ();
				var arguments = new List<AttributeArgumentNodeBase> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					arguments.Add (AttributeArgumentNodeBase.Create (argNav));
				}
				var properties = new List<AttributePropertyNode> ();
				foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
					var prop = new AttributePropertyNode (name: GetName (propertyNav), value: propertyNav.Value);
					properties.Add (prop);
				}
				var fields = new List<AttributeFieldNode> ();
				foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
					var field = new AttributeFieldNode (name: GetName (fieldNav), value: fieldNav.Value);
					fields.Add (field);
				}
				FullName = GetFullName (nav);
				Internal = GetAttribute (nav, "internal");
				Assembly = GetAttribute (nav, AssemblyElementName);
				Arguments = arguments;
				Properties = properties;
				Fields = fields;
				Diagnostics = diagnostics;
			}
		}

		public abstract record AttributeArgumentNodeBase : NodeBase
		{
			protected internal AttributeArgumentNodeBase (XPathNavigator nav) : base (nav)
			{ }

			internal static AttributeArgumentNodeBase Create (XPathNavigator nav)
			{
				var type = nav.GetAttribute ("type", "");
				if (type == "System.Object") {
					return new AttributeArgumentBoxNode (nav);
				}
				// TODO: Add argument array support
				return new AttributeArgumentNode (nav);
			}
		}

		public record AttributeArgumentBoxNode : AttributeArgumentNodeBase
		{
			public Type Type = typeof (System.Object);
			public AttributeArgumentNode? InnerArgument;

			public AttributeArgumentBoxNode (XPathNavigator nav) : base (nav)
			{
				var diagnostics = new List<ParsingDiagnostic> ();
				foreach (XPathNavigator innerArgNav in nav.SelectChildren (ArgumentElementName, "")) {
					if (Create (innerArgNav) is AttributeArgumentNode innerArg) {
						InnerArgument = innerArg;
						return;
					} else {
						diagnostics.Add (new ParsingDiagnostic (
								DiagnosticId.CustomAttributeArgumentForTypeRequiresNestedNode,
								nav.GetAttribute ("type", ""), "argument"));
					}
				}
				Diagnostics = diagnostics;
				// No argument child in <argument type="System.Object>
			}
		}

		public record AttributeArgumentNode : AttributeArgumentNodeBase
		{
			public string Type;
			public string Value;

			public AttributeArgumentNode (XPathNavigator nav) : base (nav)
			{
				Type = nav.GetAttribute ("type", "");
				Value = nav.Value;
			}
		}

		public record AttributePropertyNode
		{
			public string Name;
			public string Value;

			public AttributePropertyNode (string name, string value)
			{
				Value = value;
				Name = name;
			}
		}

		public record AttributeFieldNode
		{
			public string Name;
			public string Value;

			public AttributeFieldNode (string name, string value)
			{
				Value = value;
				Name = name;
			}
		}

		public partial record TypeMemberNode : FeatureSwitchedNode
		{
			public string Name;
			public string Signature;

			public TypeMemberNode (XPathNavigator nav) : base (nav)
			{
				Name = GetName (nav);
				Signature = GetSignature (nav);
			}
		}

		public partial record ParameterNode : AttributeTargetNode
		{
			public string Name;
			public ParameterNode (XPathNavigator nav) : base (nav)
			{
				Name = GetName (nav);
			}
		}

		public partial record MethodNode : TypeMemberNode
		{
			public List<ParameterNode> Parameters;
			public List<AttributeNode> ReturnAttributes;
			public MethodNode (XPathNavigator nav) : base (nav)
			{
				var diagnostics = new List<ParsingDiagnostic> ();
				Parameters = new List<ParameterNode> ();
				ReturnAttributes = new List<AttributeNode> ();
				foreach (XPathNavigator parameterNav in nav.SelectChildren (ParameterElementName, "")) {
					Parameters.Add (new ParameterNode (parameterNav));
				}
				int returnElements = 0;
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					returnElements++;
					foreach (XPathNavigator attributeNav in returnNav.SelectChildren (AttributeElementName, "")) {
						ReturnAttributes.Add (new AttributeNode (attributeNav));
					}
				}
				if (returnElements > 1)
					diagnostics.Add (new ParsingDiagnostic (DiagnosticId.XmlMoreThanOneReturnElementForMethod, Name));
				foreach (var duplicateParam in Parameters.GroupBy (p => p.Name)
					.Where (names => names.Count () > 1)
					.Select (group => group.Key)) {
					diagnostics.Add (new ParsingDiagnostic (DiagnosticId.XmlMoreThanOneValueForParameterOfMethod, duplicateParam, Name));
				}
				Diagnostics = diagnostics;
			}
		}

		public abstract record TypeNodeBase : FeatureSwitchedNode
		{
			public List<TypeMemberNode> Events;
			public List<TypeMemberNode> Fields;
			public List<TypeMemberNode> Properties;
			public List<MethodNode> Methods;
			public List<NestedTypeNode> Types;
			public TypeNodeBase (XPathNavigator nav) : base (nav)
			{
				Methods = new List<MethodNode> ();
				Properties = new List<TypeMemberNode> ();
				Fields = new List<TypeMemberNode> ();
				Events = new List<TypeMemberNode> ();
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
					Methods.Add (new MethodNode (methodNav));
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
					Properties.Add (new TypeMemberNode (propertyNav));
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
					Events.Add (new TypeMemberNode (eventNav));
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
					Fields.Add (new TypeMemberNode (fieldNav));
				}
				Types = new List<NestedTypeNode> ();
				foreach (XPathNavigator nestedTypeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (new NestedTypeNode (nestedTypeNav));
				}
			}
		}
		public partial record NestedTypeNode : TypeNodeBase
		{
			public string Name;
			public NestedTypeNode (XPathNavigator nav) : base (nav)
			{
				Name = GetName (nav);
			}
		}

		public partial record TypeNode : TypeNodeBase, ITopLevelNode
		{
			public string FullName;
			public TypeNode (XPathNavigator nav) : base (nav)
			{
				FullName = GetFullName (nav);
			}
		}

		public record AssemblyNode : FeatureSwitchedNode, ITopLevelNode
		{
			public List<TypeNode> Types;
			public string FullName;
			public AssemblyNode (XPathNavigator nav) : base (nav)
			{
				FullName = GetFullName (nav);
				Types = new List<TypeNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (new TypeNode (typeNav));
				}
			}
		}

		public interface ITopLevelNode
		{
		}
		public record LinkerNode : NodeBase
		{
			public List<TypeNode> Types;
			public List<AssemblyNode> Assemblies;
			internal LinkerNode (XPathNavigator nav) : base (nav)
			{
				Types = new List<TypeNode> ();
				Assemblies = new List<AssemblyNode> ();
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace)) {
					Diagnostics = new ParsingDiagnostic[] { new ParsingDiagnostic (DiagnosticId.ErrorProcessingXmlLocation, $"XML does not have <{LinkerElementName}> base tag") };
					return;
				}
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					Types.Add (new TypeNode (typeNav));
				}
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					Assemblies.Add (new AssemblyNode (assemblyNav));
				}
			}
		}
		public abstract record NodeBase : XmlProcessorBase
		{
			public IXmlLineInfo? LineInfo;
			public IEnumerable<ParsingDiagnostic>? Diagnostics;
			protected NodeBase (XPathNavigator nav)
			{
				LineInfo = (nav is IXmlLineInfo lineInfo) ? lineInfo : null;
			}
		}

		public abstract record AttributeTargetNode : NodeBase
		{
			public List<AttributeNode> Attributes;

			public AttributeTargetNode (XPathNavigator nav) : base (nav)
			{
				var attributes = new List<AttributeNode> ();
				foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
					var attr = new AttributeNode (attributeNav);
					if (attr == null)
						continue;
					attributes.Add (attr);
				}
				Attributes = attributes;
			}
		}

		public abstract record FeatureSwitchedNode : AttributeTargetNode
		{
			public FeatureSwitch? FeatureSwitch;

			public FeatureSwitchedNode (XPathNavigator nav) : base (nav)
			{
				string? feature = GetAttribute (nav, "feature");
				if (string.IsNullOrEmpty (feature))
					feature = null;
				string? featurevalue = GetAttribute (nav, "featurevalue");
				if (string.IsNullOrEmpty (featurevalue))
					featurevalue = null;
				string? featuredefault = GetAttribute (nav, "featuredefault");
				if (string.IsNullOrEmpty (featuredefault))
					featuredefault = null;
				if (feature is null && featurevalue is null && featuredefault is null) {
					return;
				}
				// if (feature is null && (featurevalue is not null || featuredefault is not null)) ;
				// error, feature must be defined if featurevalue or featuredefault is defined
				FeatureSwitch = new FeatureSwitch (feature, featurevalue, featuredefault);
			}
		}

		public record FeatureSwitch
		{
			public string? Feature;
			public string? FeatureValue;
			public string? FeatureDefault;
			public FeatureSwitch (string? feature, string? featureValue, string? featureDefault)
			{
				Feature = feature;
				FeatureValue = featureValue;
				FeatureDefault = featureDefault;
			}
		}

		public record struct ParsingDiagnostic
		{
			public DiagnosticId DiagnosticId;
			public string[] MessageArgs;

			internal ParsingDiagnostic (DiagnosticId diag, params string[] args)
			{
				DiagnosticId = diag;
				MessageArgs = args;
			}
		}
	}
}