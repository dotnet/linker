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

			var xml = false;
			foreach (var attribute in provider.CustomAttributes) {
				if (!xml && IsPreservedXmlSerializationAttribute (attribute))
					xml = true;
				// TODO: other serializers
			}
			if (!xml)
				return;

			TypeDefinition type = provider switch {
				TypeDefinition td => td,
				FieldDefinition field => field.DeclaringType,
				PropertyDefinition property => property.DeclaringType,
				EventDefinition @event => @event.DeclaringType,
				_ => throw new ArgumentException ($"{nameof (provider)} has invalid provider type {provider.GetType ()}")
			};

			if (xml)
				Context.SerializationHelper.MarkRecursiveMembers (type, new DependencyInfo (DependencyKind.XmlSerialized, provider));
		}

		static bool IsPreservedXmlSerializationAttribute (CustomAttribute attribute)
		{
			var type = attribute.Constructor.DeclaringType;
			var name = type.Name;
			return type.Namespace == "System.Xml.Serialization"
				&& name.StartsWith ("Xml", StringComparison.Ordinal)
				&& name.EndsWith ("Attribute", StringComparison.Ordinal)
				&& name != "XmlIgnoreAttribute";
		}
	}
}