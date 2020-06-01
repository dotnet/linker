// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;
using System.Linq;

namespace Mono.Linker.Dataflow
{
	class CustomAttributeSource
	{
		private readonly List<XmlFlowAnnotationSource> _sources;

		public CustomAttributeSource (LinkContext context)
		{
			List<XmlFlowAnnotationSource> annotationSources = new List<XmlFlowAnnotationSource> ();
			if (context.AttributeDefinitions?.Count > 0) {
				foreach (string a in context.AttributeDefinitions) {
					XmlFlowAnnotationSource xmlAnnotations = new XmlFlowAnnotationSource (context);
					xmlAnnotations.ParseXml (a);
					annotationSources.Add (xmlAnnotations);
				}
			}
			_sources = annotationSources;
		}

		public IEnumerable<CustomAttribute> GetCustomAttributes (ICustomAttributeProvider provider)
		{
			if (_sources.Count > 0) {
				foreach (var source in _sources) {
					if (source.HasCustomAttributes (provider)) {
						foreach (var customAttribute in source.GetCustomAttributes (provider))
							yield return customAttribute;
					}
				}
			}
			if (provider.HasCustomAttributes) {
				foreach (var customAttribute in provider.CustomAttributes)
					yield return customAttribute;
			}
		}

		public bool HasCustomAttributes (ICustomAttributeProvider provider)
		{
			if (_sources != null) {
				foreach (var source in _sources) {
					if (source.HasCustomAttributes (provider)) {
						return true;
					}
				}
			}
			return provider.HasCustomAttributes;
		}
	}
}
