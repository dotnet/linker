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
		static bool ShouldProcessElement (XPathNavigator nav)
		{
			string? feature = GetAttribute (nav, "feature");
			string? featurevalue = GetAttribute (nav, "featurevalue");
			string? featuredefault = GetAttribute (nav, "featuredefault");
			if (feature == null) return true;

			bool.TryParse (featuredefault, out var defaultVal);
			if (featurevalue == featuredefault || (featurevalue == null && defaultVal == false)) return true;
			return false;
		}

		public static Dictionary<InjectedType, List<InjectedAttribute>> ProcessXml (CompilationStartAnalysisContext compilationContext)
		{
			var injectionContext = new InjectionContext ();
			var _injections = new Dictionary<InjectedType, List<InjectedAttribute>> ();
			Stream? stream = GenerateStream ("ILLink.LinkAttributes.xml", compilationContext);
			if (stream == null)
				return _injections;
			XPathDocument doc = new XPathDocument (stream);
			try {
				XPathNavigator nav = doc.CreateNavigator ();


				if (!nav.MoveToChild (LinkerElementName, XmlNamespace))
					return _injections;

				if (nav.SchemaInfo.Validity == XmlSchemaValidity.Invalid) { }
				// Warn about invalid xml according to schema

				if (!ShouldProcessElement (nav))
					return _injections;

				ProcessAssemblies (nav, injectionContext);
			}
			// TODO: handle the correct exceptions correctly
			catch (Exception) {
				throw new NotImplementedException ("Processing XML failed and was not handled");
			}
			return _injections;

			void ProcessAssemblies (XPathNavigator nav, InjectionContext context)
			{
				foreach (XPathNavigator assemblyNav in nav.SelectChildren (AssemblyElementName, "")) {
					// Errors for invalid assembly names should show up even if this element will be
					// skipped due to feature conditions.
					bool processAllAssemblies = ShouldProcessAllAssemblies (assemblyNav);

					if (!ShouldProcessElement (assemblyNav))
						continue;
					ProcessAssembly (assemblyNav, context);
				}
			}

			void ProcessAssembly (XPathNavigator nav, InjectionContext context)
			{
				if (!ShouldProcessElement (nav))
					return;
				var newContext = new InjectionContext (GetFullName (nav), context.Path);
				ProcessAttributes (nav, newContext, AttributeTargets.Assembly);
				ProcessTypes (nav, newContext);
			}

			/// <summary>
			///		Takes an XPathNavigator that might have <type> children and processes those children
			/// </summary>
			void ProcessTypes (XPathNavigator nav, InjectionContext context)
			{
				foreach (XPathNavigator typeNav in nav.SelectChildren (TypeElementName, "")) {
					if (ShouldProcessElement (typeNav))
						ProcessType (typeNav, context);
				}
			}

			void ProcessType (XPathNavigator nav, InjectionContext context)
			{
				var path = GetFullName (nav);
				if (path == "") path = context.Path + GetName (nav);
				var childContext = new InjectionContext (context.Assembly, path);
				foreach (XPathNavigator methodNav in nav.SelectChildren (MethodElementName, "")) {
					if(ShouldProcessElement(methodNav))
						ProcessMethod (methodNav, childContext);
				}
				foreach (XPathNavigator propertyNav in nav.SelectChildren (PropertyElementName, "")) {
					if(ShouldProcessElement(propertyNav))
						ProcessMember (propertyNav, childContext, AttributeTargets.Property);
				}
				foreach (XPathNavigator eventNav in nav.SelectChildren (EventElementName, "")) {
					if (ShouldProcessElement (eventNav))
						ProcessMember (eventNav, childContext, AttributeTargets.Event);
				}
				foreach (XPathNavigator fieldNav in nav.SelectChildren (FieldElementName, "")) {
					if (ShouldProcessElement (fieldNav))
						ProcessMember (fieldNav, childContext, AttributeTargets.Field);
				}

				ProcessTypes (nav, childContext);
				ProcessAttributes (nav, childContext, AttributeTargets.Class);
			}

			void ProcessMember(XPathNavigator nav, InjectionContext context, AttributeTargets kind)
			{
				var newContext = new InjectionContext (context.Assembly, context.Path + "." + GetName (nav));
				ProcessAttributes (nav, newContext, kind);
			}

			void ProcessMethod (XPathNavigator nav, InjectionContext context)
			{
				var newContext = new InjectionContext (assembly: context.Assembly, path: context.Path + "." + GetName(nav));
				ProcessAttributes (nav, context, AttributeTargets.Method);
				foreach (XPathNavigator parameterNav in nav.SelectChildren(ParameterElementName,"")) {
					if (ShouldProcessElement (parameterNav))
						ProcessMember (parameterNav, newContext, AttributeTargets.Parameter);
				}
				foreach (XPathNavigator returnNav in nav.SelectChildren (ReturnElementName, "")) {
					if (ShouldProcessElement (returnNav)) {
						var returnContext = new InjectionContext (context.Assembly, context.Path + "." + GetName (nav));
						ProcessAttributes (nav, returnContext, AttributeTargets.ReturnValue);
					}
				}
			}

			void ProcessAttributes (XPathNavigator nav, InjectionContext context, AttributeTargets kind)
			{
				var attributes = new List<InjectedAttribute> ();
				foreach (XPathNavigator attributeNav in nav.SelectChildren (AttributeElementName, "")) {
					if (!ShouldProcessElement (attributeNav))
						continue;
					var attr = ProcessAttribute (attributeNav, context);
					if (attr == null)
						continue;
					attributes.Add(attr);
				}
				var type = new InjectedType (name: context.Path, AttributeTargets: kind, assembly: context.Assembly);
				_injections.Add (type, attributes);
			}

			InjectedAttribute? ProcessAttribute (XPathNavigator nav, InjectionContext context)
			{
				if (!ShouldProcessElement (nav)) return null;
				var args = new List<InjectedAttributeArgument> ();
				foreach (XPathNavigator argNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (argNav, "type");
					if (type == "") type = "System.String";
					var arg = new InjectedAttributeArgument (type, argNav.Value);
					args.Add (arg);
				}
				var properties = new List<InjectedAttributeProperty> ();
				foreach (XPathNavigator propertyNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (propertyNav, "type");
					if (type == "") type = "System.String";
					var prop = new InjectedAttributeProperty (name: GetName(propertyNav), type: type, value: propertyNav.Value);
					properties.Add (prop);
				}
				var fields = new List<InjectedAttributeField> ();
				foreach (XPathNavigator fieldNav in nav.SelectChildren (ArgumentElementName, "")) {
					var type = GetAttribute (fieldNav, "type");
					if (type == "") type = "System.String";
					var field = new InjectedAttributeField (name: GetName (fieldNav), type: type, value: fieldNav.Value);
					fields.Add (field);
				}
				return new InjectedAttribute (fullname: GetFullName (nav), assembly: context.Assembly, args: args.ToArray (), fields: fields.ToArray (), props: properties.ToArray ());
			}
		}
	}



	public class InjectedAttribute
	{
		public readonly string Fullname;
		public readonly string Assembly;
		public readonly InjectedAttributeArgument[] Args;
		public readonly InjectedAttributeProperty[] Props;
		public readonly InjectedAttributeField[] Fields;

		public InjectedAttribute (string fullname, string assembly, InjectedAttributeArgument[] args, InjectedAttributeField[] fields, InjectedAttributeProperty[] props)
		{
			Fullname = fullname;
			Assembly = assembly;
			Args = args;
			Fields = fields;
			Props = props;
		}
	}

	public struct InjectedAttributeArgument
	{
		public readonly string Type;
		public readonly string Value;

		public InjectedAttributeArgument (string type, string value)
		{
			Type = type;
			Value = value;
		}
	}
	public struct InjectedAttributeProperty
	{
		public readonly string Name;
		public readonly string Type;
		public readonly string Value;

		public InjectedAttributeProperty (string name, string type, string value)
		{
			Type = type;
			Value = value;
			Name = name;
		}
	}
	public struct InjectedAttributeField
	{
		public readonly string Name;
		public readonly string Type;
		public readonly string Value;

		public InjectedAttributeField (string name, string type, string value)
		{
			Type = type;
			Value = value;
			Name = name;
		}
	}

	public class InjectedType
	{
		public readonly AttributeTargets AttributeTargets;
		public readonly string Fullname;
		public readonly string Assembly;

		public InjectedType (string name, AttributeTargets attributeTargets, string assembly)
		{
			Fullname = name;
			AttributeTargets = attributeTargets;
			Assembly = assembly;
		}
	}
	internal readonly struct InjectionContext
	{
		public readonly string Assembly;
		public readonly string Path;

		internal InjectionContext (string assembly, string path)
		{
			Path = path;
			Assembly = assembly;
		}
		public InjectionContext ()
		{
			Path = "";
			Assembly = "";
		}
	}
}