// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using Mono.Linker.Steps;

namespace Mono.Linker
{

	// This discovers types attributed with certain serialization attributes, to match the old behavior
	// of xamarin-android. It is not meant to be complete. Unlike xamarin-andorid:
	// - this will only discover attributed types in marked assemblies
	// - this will discover types in non-"link" assemblies as well
	public class PreserveSerialization : BaseSubStep
	{

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			// TODO: check for referenced assembly?
			return true;
		}

		public override SubStepTargets Targets =>
			SubStepTargets.Type
			| SubStepTargets.Field
			| SubStepTargets.Method
			| SubStepTargets.Property
			| SubStepTargets.Event;

		public override void ProcessType (TypeDefinition type)
		{
			ProcessAttributeProvider (type);
		}

		public override void ProcessField (FieldDefinition field)
		{
			ProcessAttributeProvider (field);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			ProcessAttributeProvider (property);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			ProcessAttributeProvider (method);
		}

		public override void ProcessEvent (EventDefinition @event)
		{
			ProcessAttributeProvider (@event);
		}

		void ProcessAttributeProvider (ICustomAttributeProvider provider)
		{
			if (!provider.HasCustomAttributes)
				return;

			var serializedFor = SerializerKind.None;

			foreach (var attribute in provider.CustomAttributes) {
				if (IsPreservedSerializationAttribute (provider, attribute, out SerializerKind serializerKind))
					serializedFor |= serializerKind;
			}

			TypeDefinition type = provider switch {
				TypeDefinition td => td,
				FieldDefinition field => field.DeclaringType,
				PropertyDefinition property => property.DeclaringType,
				EventDefinition @event => @event.DeclaringType,
				MethodDefinition method => method.DeclaringType,
				_ => throw new ArgumentException ($"{nameof (provider)} has invalid provider type {provider.GetType ()}")
			};

			if (serializedFor.HasFlag (SerializerKind.DataContractSerializer))
				Context.SerializationHelper.MarkRecursiveMembers (type, new DependencyInfo (DependencyKind.DataContractSerialized, provider));
			if (serializedFor.HasFlag (SerializerKind.XmlSerializer))
				Context.SerializationHelper.MarkRecursiveMembers (type, new DependencyInfo (DependencyKind.XmlSerialized, provider));
		}

		enum SerializerKind
		{
			None,
			XmlSerializer,
			DataContractSerializer,
		}

		static bool IsPreservedSerializationAttribute (ICustomAttributeProvider provider, CustomAttribute attribute, out SerializerKind serializerKind)
		{
			TypeReference attributeType = attribute.Constructor.DeclaringType;
			serializerKind = SerializerKind.None;

			switch (attributeType.Namespace) {

			// http://bugzilla.xamarin.com/show_bug.cgi?id=1415
			// http://msdn.microsoft.com/en-us/library/system.runtime.serialization.datamemberattribute.aspx
			case "System.Runtime.Serialization":
				var serialized = false;
				if (provider is PropertyDefinition or FieldDefinition or EventDefinition)
					serialized = attributeType.Name == "DataMemberAttribute";
				else if (provider is TypeDefinition)
					serialized = attributeType.Name == "DataContractAttribute";

				if (serialized) {
					serializerKind = SerializerKind.DataContractSerializer;
					return true;
				}
				break;

			// http://msdn.microsoft.com/en-us/library/83y7df3e.aspx
			case "System.Xml.Serialization":
				var attributeName = attributeType.Name;
				if (attributeName.StartsWith ("Xml", StringComparison.Ordinal)
					&& attributeName.EndsWith ("Attribute", StringComparison.Ordinal)
					&& attributeName != "XmlIgnoreAttribute") {
					serializerKind = SerializerKind.XmlSerializer;
					return true;
				}
				break;

			};

			return false;
		}
	}
}