// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	struct LinkerAttributesInformation
	{
		Dictionary<Type, object> _linkerAttributes;

		public void InitializeForMethod (MethodDefinition method)
		{
			if (method.HasCustomAttributes)
				foreach (var customAttribute in method.CustomAttributes) {
					var attributeType = customAttribute.AttributeType;
					if (attributeType.Name == "RequiresUnreferencedCodeAttribute" && attributeType.Namespace == "System.Diagnostics.CodeAnalysis") {
						AddAttribute (ProcessRequiresUnreferencedCodeAttribute (customAttribute));
					}
				}
		}

		public bool TryGetAttribute<T> (out T attributeValue) where T : class
		{
			attributeValue = null;

			if (_linkerAttributes != null && _linkerAttributes.TryGetValue (typeof (T), out var returnValue)) {
				attributeValue = (T) returnValue;
				return true;
			}

			return false;
		}

		void AddAttribute (object attribute)
		{
			if (attribute == null)
				return;

			if (_linkerAttributes == null)
				_linkerAttributes = new Dictionary<Type, object> ();

			_linkerAttributes.Add (attribute.GetType (), attribute);
		}

		static object ProcessRequiresUnreferencedCodeAttribute (CustomAttribute customAttribute)
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
