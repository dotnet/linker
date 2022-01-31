// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace ILLink.Shared
{
	public static class LinkAttributes
	{
		public static List<IRootNode> ProcessXml (XDocument doc)
		{
			try {
				XPathNavigator nav = doc.CreateNavigator ();
				return NodeBase.ProcessRootNodes (nav);
			}
			// TODO: handle the correct exceptions correctly
			catch (ArgumentException) {
				// XML doesn't have a <linker> tag and is invalid. The document should have been validated before 
				return new List<IRootNode> ();
			}
		}

		public record AttributeNode : NodeBase
		{
			public string FullName;
			public string Internal;
			public string Assembly;
			public List<IAttributeArgumentNode> Arguments;
			public List<AttributePropertyNode> Properties;
			public List<AttributeFieldNode> Fields;

			public AttributeNode (XPathNavigator nav) : base (nav)
			{
				var arguments = new List<IAttributeArgumentNode> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					if (ProcessArgument (argNav) is AttributeArgumentNode arg) {
						arguments.Add (arg);
					}
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

				static IAttributeArgumentNode ProcessArgument (XPathNavigator argNav)
				{
					var type = argNav.GetAttribute ("type", "");
					if (type == "") type = "System.String";
					if (type == "System.Object") {
						foreach (XPathNavigator innerArgNav in argNav.SelectChildren (ArgumentElementName, "")) {
							if (ProcessArgument (innerArgNav) is AttributeArgumentNode innerArg) {
								return new AttributeArgumentBoxNode (innerArg);
							} else {
								// error
								throw new ArgumentException ("Arguments to attribute cannot be wrapped in more than one type");
							}
						}
						// No argument child in <argument type="System.Object>
						return new AttributeArgumentBoxNode (null);
					}
					var arg = new AttributeArgumentNode (type, argNav.Value);
					return arg;
				}
			}
		}

		public interface IAttributeArgumentNode { }

		public record AttributeArgumentBoxNode : IAttributeArgumentNode
		{
			public Type Type = typeof (System.Object);
			public AttributeArgumentNode? InnerArgument;

			public AttributeArgumentBoxNode (AttributeArgumentNode? innerArgument)
			{
				InnerArgument = innerArgument;
			}
		}

		public record AttributeArgumentNode : IAttributeArgumentNode
		{
			public string Type;
			public string Value;

			public AttributeArgumentNode (string type, string value)
			{
				Type = type;
				Value = value;
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
				Parameters = new List<ParameterNode> ();
				ReturnAttributes = new List<AttributeNode> ();
				foreach (XPathNavigator parameterNav in nav.SelectChildren (ParameterElementName, "")) {
					Parameters.Add (new ParameterNode (parameterNav));
				}
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					foreach (XPathNavigator attributeNav in returnNav.SelectChildren (AttributeElementName, "")) {
						ReturnAttributes.Add (new AttributeNode (attributeNav));
					}
				}
			}
		}

		public abstract record TypeBaseNode : FeatureSwitchedNode
		{
			public List<TypeMemberNode> Events;
			public List<TypeMemberNode> Fields;
			public List<TypeMemberNode> Properties;
			public List<MethodNode> Methods;
			public List<NestedTypeNode> Types;
			public TypeBaseNode (XPathNavigator nav) : base (nav)
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
		public partial record NestedTypeNode : TypeBaseNode
		{
			public string Name;
			public NestedTypeNode (XPathNavigator nav) : base (nav)
			{
				Name = GetName (nav);
			}
		}

		public partial record TypeNode : TypeBaseNode, IRootNode
		{
			public string FullName;
			public TypeNode (XPathNavigator nav) : base (nav)
			{
				FullName = GetFullName (nav);
			}
		}

		public record AssemblyNode : FeatureSwitchedNode, IRootNode
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

		public interface IRootNode
		{
		}

		public abstract record NodeBase : XmlProcessorBase
		{
			public IXmlLineInfo? LineInfo;
			protected NodeBase (XPathNavigator nav)
			{
				LineInfo = (nav is IXmlLineInfo lineInfo) ? lineInfo : null;
			}

			public static List<IRootNode> ProcessRootNodes (XPathNavigator nav)
			{
				if (!nav.MoveToChild (LinkerElementName, XmlNamespace)) {
					throw new ArgumentException ($"XML does not have <{LinkerElementName}> base tag");
				}
				var roots = new List<IRootNode> ();
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					roots.Add (new TypeNode (typeNav));
				}
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					roots.Add (new AssemblyNode (assemblyNav));
				}
				return roots;
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
	}
}