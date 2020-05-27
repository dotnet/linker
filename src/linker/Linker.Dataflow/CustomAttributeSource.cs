// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;
using System.Collections.ObjectModel;
using System.Linq;

namespace Mono.Linker.Dataflow
{
	class CustomAttributeSource
	{
		private readonly XmlFlowAnnotationSource[] _sources;

		public CustomAttributeSource (LinkContext _context)
		{
			Collection<XmlFlowAnnotationSource> annotationSources = new Collection<XmlFlowAnnotationSource> ();
			if (_context.AttributeDefinitions != null && _context.AttributeDefinitions.Count > 0) {
				foreach (string a in _context.AttributeDefinitions) {
					XmlFlowAnnotationSource xmlAnnotations = new XmlFlowAnnotationSource (_context);
					xmlAnnotations.ParseXml (a);
					annotationSources.Add (xmlAnnotations);
				}
			}
			_sources = annotationSources.ToArray ();
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			IEnumerable<CustomAttribute> aggregateAttributes = null;
			foreach (var source in _sources) {
				if (source.HasCustomAttributes (provider))
					aggregateAttributes = aggregateAttributes == null ? source.GetCustomAttributes (provider) : aggregateAttributes.Concat (source.GetCustomAttributes (provider));
			}
			if (provider.HasCustomAttributes)
				aggregateAttributes = aggregateAttributes == null ? provider.CustomAttributes : aggregateAttributes.Concat (provider.CustomAttributes);
			return aggregateAttributes ?? Enumerable.Empty<CustomAttribute>();
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			foreach (var source in _sources) {
				if (source.HasCustomAttributes (provider)) {
					return true;
				}
			}
			if (provider.HasCustomAttributes) {
				return true;
			}
			return false;
		}
	}
}
