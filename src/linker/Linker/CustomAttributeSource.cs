// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	public class CustomAttributeSource
	{
		private readonly Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> _xmlCustomAttributes;
		private readonly Dictionary<ICustomAttributeProvider, IEnumerable<Attribute>> _internalAttributes;

		readonly HashSet<AssemblyDefinition> _processedAttributeXml;
		readonly LinkContext _context;

		public CustomAttributeSource (LinkContext context)
		{
			_xmlCustomAttributes = new Dictionary<ICustomAttributeProvider, IEnumerable<CustomAttribute>> ();
			_internalAttributes = new Dictionary<ICustomAttributeProvider, IEnumerable<Attribute>> ();
			_processedAttributeXml = new HashSet<AssemblyDefinition> ();
			_context = context;
		}


		public static AssemblyDefinition GetAssemblyFromCustomAttributeProvider (ICustomAttributeProvider provider)
		{
			return provider switch
			{
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

		public void EnsureProcessedAttributeXml (ICustomAttributeProvider provider)
		{
			var assembly = GetAssemblyFromCustomAttributeProvider (provider);

			if (!_processedAttributeXml.Add (assembly))
				return;

			EmbeddedXmlInfo.ProcessAttributes (assembly, _context);
		}

		public void AddCustomAttributes (ICustomAttributeProvider provider, IEnumerable<CustomAttribute> customAttributes)
		{
			if (!_xmlCustomAttributes.ContainsKey (provider))
				_xmlCustomAttributes[provider] = customAttributes;
			else
				_xmlCustomAttributes[provider] = _xmlCustomAttributes[provider].Concat (customAttributes);
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}

			EnsureProcessedAttributeXml (provider);

			if (_xmlCustomAttributes.TryGetValue (provider, out var annotations)) {
				foreach (var customAttribute in annotations)
					yield return customAttribute;
			}
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			if (provider.HasCustomAttributes)
				return true;

			EnsureProcessedAttributeXml (provider);

			if (_xmlCustomAttributes.ContainsKey (provider))
				return true;

			return false;
		}

		public void AddInternalAttributes (ICustomAttributeProvider provider, IEnumerable<Attribute> attributes)
		{
			if (!_internalAttributes.ContainsKey (provider))
				_internalAttributes[provider] = attributes;
			else
				_internalAttributes[provider] = _internalAttributes[provider].Concat (attributes);
		}

		public IEnumerable<Attribute> GetInternalAttributes (ICustomAttributeProvider provider)
		{
			EnsureProcessedAttributeXml (provider);

			if (_internalAttributes.TryGetValue (provider, out var annotations)) {
				foreach (var attribute in annotations)
					yield return attribute;
			}
		}

		public bool HasInternalAttributes (ICustomAttributeProvider provider)
		{
			EnsureProcessedAttributeXml (provider);

			return _internalAttributes.ContainsKey (provider) ? true : false;
		}

		public bool HasAttributes (ICustomAttributeProvider provider) =>
			HasCustomAttributes (provider) || HasInternalAttributes (provider);
	}
}
