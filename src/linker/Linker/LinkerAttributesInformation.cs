// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker
{
	readonly struct LinkerAttributesInformation
	{
		readonly Dictionary<Type, List<object>> _linkerAttributes;

		public LinkerAttributesInformation (LinkContext context, IMemberDefinition member)
		{
			_linkerAttributes = null;

			if (member.HasCustomAttributes) {
				foreach (var customAttribute in member.CustomAttributes) {
					var attributeType = customAttribute.AttributeType;
					object attributeValue = null;
					if (IsAttribute<RequiresUnreferencedCodeAttribute> (attributeType))
						attributeValue = ProcessRequiresUnreferencedCodeAttribute (context, member, customAttribute);
					else if (IsAttribute<DynamicDependencyAttribute> (attributeType))
						attributeValue = ProcessDynamicDependencyAttribute (context, member, customAttribute);
					AddAttribute (ref _linkerAttributes, attributeValue);
				}
			}
		}

		public LinkerAttributesInformation (LinkContext context, FieldDefinition field)
		{
			_linkerAttributes = null;

			if (field.HasCustomAttributes) {
				foreach (var customAttribute in field.CustomAttributes) {
					var attributeType = customAttribute.AttributeType;
					object attributeValue = null;
					if (IsAttribute<DynamicDependencyAttribute> (attributeType))
						attributeValue = ProcessDynamicDependencyAttribute (context, field, customAttribute);
					AddAttribute (ref _linkerAttributes, attributeValue);
				}
			}
		}

		static void AddAttribute (ref Dictionary<Type, List<object>> attributes, object attributeValue)
		{
			if (attributeValue == null)
				return;

			if (attributes == null)
				attributes = new Dictionary<Type, List<object>> ();

			Type attributeValueType = attributeValue.GetType ();
			if (!attributes.TryGetValue (attributeValueType, out var attributeList)) {
				attributeList = new List<object> ();
				attributes.Add (attributeValueType, attributeList);
			}

			attributeList.Add (attributeValue);
		}

		public bool HasAttribute<T> ()
		{
			return _linkerAttributes != null && _linkerAttributes.ContainsKey (typeof (T));
		}

		public IEnumerable<T> GetAttributes<T> ()
		{
			if (_linkerAttributes == null || !_linkerAttributes.TryGetValue (typeof (T), out var attributeList))
				return Enumerable.Empty<T> ();

			if (attributeList == null || attributeList.Count == 0) {
				throw new LinkerFatalErrorException ("Unexpected list of attributes.");
			}

			return attributeList.Cast<T> ();
		}

		public static bool IsAttribute<T> (TypeReference tr)
		{
			var type = typeof (T);
			return tr.Name == type.Name && tr.Namespace == tr.Namespace;
		}

		static Attribute ProcessRequiresUnreferencedCodeAttribute (LinkContext context, IMemberDefinition method, CustomAttribute customAttribute)
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

			context.LogWarning ($"Attribute '{typeof (RequiresUnreferencedCodeAttribute).FullName}' on '{method}' doesn't have a required constructor argument.",
				2028, MessageOrigin.TryGetOrigin (method, 0));
			return null;
		}

		static DynamicDependency ProcessDynamicDependencyAttribute (LinkContext context, IMemberDefinition member, CustomAttribute customAttribute)
		{
			if (!ShouldProcessDependencyAttribute (context, customAttribute))
				return null;

			var dynamicDependency = GetDynamicDependency (context, customAttribute);
			if (dynamicDependency == null) {
				context.LogMessage (MessageContainer.CreateWarningMessage (context,
					$"Invalid DynamicDependencyAttribute on '{member}'",
					2034, MessageOrigin.TryGetOrigin (member)));
				return null;
			}

			dynamicDependency.OriginalAttribute = customAttribute;
			return dynamicDependency;
		}

		static DynamicDependency GetDynamicDependency (LinkContext context, CustomAttribute ca)
		{
			var args = ca.ConstructorArguments;
			if (args.Count > 3)
				return null;

			// First argument is string or DynamicallyAccessedMemberTypes
			string memberSignature = args[0].Value as string;
			if (args.Count == 1)
				return memberSignature == null ? null : new DynamicDependency (memberSignature);
			DynamicallyAccessedMemberTypes? memberTypes = null;
			if (memberSignature == null) {
				var argType = args[0].Type;
				if (!(argType.Namespace == "System.Diagnostics.CodeAnalysis" && argType.Name == "DynamicallyAccessedMemberTypes"))
					return null;
				try {
					memberTypes = (DynamicallyAccessedMemberTypes) args[0].Value;
				} catch (InvalidCastException) { }
				if (memberTypes == null)
					return null;
			}

			// Second argument is Type for ctors with two args, string for ctors with three args
			if (args.Count == 2) {
				if (!(args[1].Value is TypeReference type))
					return null;
				return memberSignature == null ? new DynamicDependency (memberTypes.Value, type) : new DynamicDependency (memberSignature, type);
			}
			Debug.Assert (args.Count == 3);
			if (!(args[1].Value is string typeName))
				return null;

			// Third argument is assembly name
			if (!(args[2].Value is string assemblyName))
				return null;

			return memberSignature == null ? new DynamicDependency (memberTypes.Value, typeName, assemblyName) : new DynamicDependency (memberSignature, typeName, assemblyName);
		}

		public static bool ShouldProcessDependencyAttribute (LinkContext context, CustomAttribute ca)
		{
			if (ca.HasProperties && ca.Properties[0].Name == "Condition") {
				var condition = ca.Properties[0].Argument.Value as string;
				switch (condition) {
				case "":
				case null:
					return true;
				case "DEBUG":
					if (!context.KeepMembersForDebugger)
						return false;

					break;
				default:
					// Don't have yet a way to match the general condition so everything is excluded
					return false;
				}
			}
			return true;
		}
	}
}
