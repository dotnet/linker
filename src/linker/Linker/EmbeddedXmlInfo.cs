// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker
{
	public static class EmbeddedXmlInfo
	{
		static EmbeddedResource GetEmbeddedXml (AssemblyDefinition assembly, Func<Resource, bool> predicate)
		{
			return assembly.Modules
				.SelectMany (mod => mod.Resources)
				.Where (res => res.ResourceType == ResourceType.Embedded)
				.Where (res => res.Name.EndsWith (".xml", StringComparison.OrdinalIgnoreCase))
				.Where (res => predicate (res))
				.SingleOrDefault () as EmbeddedResource;
		}

		public static void ProcessDescriptors (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return;

			var rsc = GetEmbeddedXml (assembly, res => ShouldProcessRootDescriptorResource (assembly, context, res.Name));
			if (rsc == null)
				return;

			ResolveFromXmlStep step = null;
			try {
				context.LogMessage ($"Processing embedded linker descriptor {rsc.Name} from {assembly.Name}");
				step = GetExternalResolveStep (rsc, assembly);
			} catch (XmlException ex) {
				/* This could happen if some broken XML file is embedded. */
				context.LogError ($"Error processing {rsc.Name}: {ex}", 1003);
			}

			if (step != null)
				step.Process (context);
		}

		public static void ProcessSubstitutions (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return;

			var rsc = GetEmbeddedXml (assembly, res => res.Name.Equals ("ILLink.Substitutions.xml", StringComparison.OrdinalIgnoreCase));
			if (rsc == null)
				return;

			BodySubstituterStep step = null;
			try {
				context.LogMessage ($"Processing embedded substitution descriptor {rsc.Name} from {assembly.Name}");
				step = GetExternalSubstitutionStep (rsc, assembly);
			} catch (XmlException ex) {
				context.LogError ($"Error processing {rsc.Name}: {ex}", 1003);
			}

			if (step != null)
				step.Process (context);
		}

		public static void ProcessAttributes (AssemblyDefinition assembly, LinkContext context)
		{
			if (context.Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return;

			var rsc = GetEmbeddedXml (assembly, res => res.Name.Equals ("ILLink.LinkAttributes.xml", StringComparison.OrdinalIgnoreCase));
			if (rsc == null)
				return;

			LinkAttributesStep step = null;
			try {
				context.LogMessage ($"Processing embedded {rsc.Name} from {assembly.Name}");
				step = GetExternalLinkAttributesStep (rsc, assembly);
			} catch (XmlException ex) {
				context.LogError ($"Error processing {rsc.Name} from {assembly.Name}: {ex}", 1003);
			}

			if (step != null)
				step.Process (context);
		}

		static string GetAssemblyName (string descriptor)
		{
			int pos = descriptor.LastIndexOf ('.');
			if (pos == -1)
				return descriptor;

			return descriptor.Substring (0, pos);
		}

		static bool ShouldProcessRootDescriptorResource (AssemblyDefinition assembly, LinkContext context, string resourceName)
		{
			if (resourceName.Equals ("ILLink.Descriptors.xml", StringComparison.OrdinalIgnoreCase))
				return true;

			if (GetAssemblyName (resourceName) != assembly.Name.Name)
				return false;

			switch (context.Annotations.GetAction (assembly)) {
			case AssemblyAction.Link:
			case AssemblyAction.AddBypassNGen:
			case AssemblyAction.AddBypassNGenUsed:
			case AssemblyAction.Copy:
				return true;
			default:
				return false;
			}
		}

		static ResolveFromXmlStep GetExternalResolveStep (EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new ResolveFromXmlStep (GetExternalDescriptor (resource), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static BodySubstituterStep GetExternalSubstitutionStep (EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new BodySubstituterStep (GetExternalDescriptor (resource), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static LinkAttributesStep GetExternalLinkAttributesStep (EmbeddedResource resource, AssemblyDefinition assembly)
		{
			return new LinkAttributesStep (GetExternalDescriptor (resource), resource, assembly, "resource " + resource.Name + " in " + assembly.FullName);
		}

		static XPathDocument GetExternalDescriptor (EmbeddedResource resource)
		{
			using (var sr = new StreamReader (resource.GetResourceStream ())) {
				return new XPathDocument (sr);
			}
		}
	}
}
