// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	public class CustomAttributeSource
	{
		public AttributeInfo PrimaryAttributeInfo { get; }
		private readonly Dictionary<AssemblyDefinition, AttributeInfo?> _embeddedXmlInfos;
		readonly LinkContext _context;

		public CustomAttributeSource (LinkContext context)
		{
			PrimaryAttributeInfo = new AttributeInfo ();
			_embeddedXmlInfos = new Dictionary<AssemblyDefinition, AttributeInfo?> ();
			_context = context;
		}

		public static AssemblyDefinition GetAssemblyFromCustomAttributeProvider (ICustomAttributeProvider provider)
		{
			return provider switch {
				MemberReference mr => mr.Module.Assembly,
				AssemblyDefinition ad => ad,
				ModuleDefinition md => md.Assembly,
				InterfaceImplementation ii => ii.InterfaceType.Module.Assembly,
				GenericParameterConstraint gpc => gpc.ConstraintType.Module.Assembly,
				ParameterDefinition pd => pd.ParameterType.Module.Assembly,
				MethodReturnType mrt => mrt.ReturnType.Module.Assembly,
				_ => throw new NotImplementedException (provider.GetType ().ToString ()),
			};
		}

		public bool TryGetEmbeddedXmlInfo (ICustomAttributeProvider provider, [NotNullWhen (true)] out AttributeInfo? xmlInfo)
		{
			AssemblyDefinition? assembly;
			try {
				// Might be the internal RemoveAttributesInstancesAttribute with no Module or Assembly
				assembly = GetAssemblyFromCustomAttributeProvider (provider);
			} catch (NullReferenceException) {
				xmlInfo = null;
				return false;
			}

			if (!_embeddedXmlInfos.TryGetValue (assembly, out xmlInfo)) {
				// Add an empty record - this prevents reentrancy
				// If the embedded XML itself generates warnings, trying to log such warning
				// may ask for attributes (suppressions) and thus we could end up in this very place again
				// So first add a dummy record and once processed we will replace it with the real data
				_embeddedXmlInfos.Add (assembly, new AttributeInfo ());
				xmlInfo = EmbeddedXmlInfo.ProcessAttributes (assembly, _context);
				_embeddedXmlInfos[assembly] = xmlInfo;
			}

			return xmlInfo != null;
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}

			if (PrimaryAttributeInfo.CustomAttributes.TryGetValue (provider, out var annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}

			if (!TryGetEmbeddedXmlInfo (provider, out var embeddedXml))
				yield break;

			if (embeddedXml.CustomAttributes.TryGetValue (provider, out annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}
		}

		public bool HasAny (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				return true;

			if (PrimaryAttributeInfo.CustomAttributes.ContainsKey (provider))
				return true;

			if (!TryGetEmbeddedXmlInfo (provider, out var embeddedXml))
				return false;

			return embeddedXml.CustomAttributes.ContainsKey (provider);
		}
	}
}
