// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	readonly struct LinkerAttributesInformation
	{
		readonly Dictionary<Type, List<Attribute>> _linkerAttributes;

		public LinkerAttributesInformation (MethodDefinition method)
		{
			_linkerAttributes = null;

			if (method.HasCustomAttributes) {
				foreach (var customAttribute in method.CustomAttributes) {
					var attributeType = customAttribute.AttributeType;
					Attribute attributeValue = null;
					if (attributeType.Name == "RequiresUnreferencedCodeAttribute" && attributeType.Namespace == "System.Diagnostics.CodeAnalysis") {
						attributeValue = ProcessRequiresUnreferencedCodeAttribute (customAttribute);
					}

					if (attributeValue != null) {
						if (_linkerAttributes == null)
							_linkerAttributes = new Dictionary<Type, List<Attribute>> ();

						Type attributeValueType = attributeValue.GetType ();
						if (!_linkerAttributes.TryGetValue (attributeValueType, out var attributeList)) {
							attributeList = new List<Attribute> ();
							_linkerAttributes.Add (attributeValueType, attributeList);
						}

						attributeList.Add (attributeValue);
					}
				}
			}
		}

		public bool HasAttribute<T> () where T : Attribute
		{
			return _linkerAttributes != null && _linkerAttributes.ContainsKey (typeof (T));
		}

		public IEnumerable<T> GetAttributes<T> () where T : Attribute
		{
			if (_linkerAttributes != null && _linkerAttributes.TryGetValue (typeof (T), out var attributeList)) {
				if (attributeList == null || attributeList.Count == 0) {
					throw new LinkerFatalErrorException (MessageContainer.CreateErrorMessage ("IL Linker has encountered an unexpected error. Please report the issue at https://github.com/mono/linker/issues \nUnexpected list of attributes.", 1012));
				}

				return attributeList.Cast<T> ();
			}

			return Enumerable.Empty<T> ();
		}

		static Attribute ProcessRequiresUnreferencedCodeAttribute (CustomAttribute customAttribute)
		{
			if (customAttribute.HasConstructorArguments) {
				string message = (string) customAttribute.ConstructorArguments[0].Value;
				string url = null;
				foreach (var prop in customAttribute.Properties) {
					if (prop.Name == "Url") {
						url = (string) prop.Argument.Value;
					}
				}

				return new RequiresUnreferencedCodeAttribute (message) { Url = url };
			}

			return null;
		}
	}
}
