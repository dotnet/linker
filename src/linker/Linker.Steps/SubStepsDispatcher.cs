// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;

using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	internal struct CategorizedSubSteps
	{
		public List<ISubStep> on_assemblies;
		public List<ISubStep> on_types;
		public List<ISubStep> on_fields;
		public List<ISubStep> on_methods;
		public List<ISubStep> on_properties;
		public List<ISubStep> on_events;
	}

	//
	// Generic steps dispatcher is intended to by used by custom linker step which
	// consist of multiple steps. It simplifies their implementation as well as the
	// way how to hook them into the pipeline of existing steps.
	//
	public abstract class SubStepsDispatcher : IStep
	{
		private readonly List<ISubStep> substeps;

		private CategorizedSubSteps? categorized;
		private CategorizedSubSteps Categorized {
			get {
				Debug.Assert (categorized.HasValue);
				return categorized.Value;
			}
		}

		protected SubStepsDispatcher ()
		{
			substeps = new List<ISubStep> ();
		}

		protected SubStepsDispatcher (IEnumerable<ISubStep> subSteps)
		{
			substeps = new List<ISubStep> (subSteps);
		}

		public void Add (ISubStep substep)
		{
			substeps.Add (substep);
		}

		public virtual void Process (LinkContext context)
		{
			InitializeSubSteps (context);

			BrowseAssemblies (context.GetAssemblies ());
		}

		private static bool HasSubSteps (List<ISubStep> substeps) => substeps?.Count > 0;

		private void BrowseAssemblies (IEnumerable<AssemblyDefinition> assemblies)
		{
			foreach (var assembly in assemblies) {
				CategorizeSubSteps (assembly);

				if (HasSubSteps (Categorized.on_assemblies))
					DispatchAssembly (assembly);

				if (!ShouldDispatchTypes ())
					continue;

				BrowseTypes (assembly.MainModule.Types);
			}
		}

		private bool ShouldDispatchTypes ()
		{
			return HasSubSteps (Categorized.on_types)
				|| HasSubSteps (Categorized.on_fields)
				|| HasSubSteps (Categorized.on_methods)
				|| HasSubSteps (Categorized.on_properties)
				|| HasSubSteps (Categorized.on_events);
		}

		private void BrowseTypes (Collection<TypeDefinition> types)
		{
			foreach (TypeDefinition type in types) {
				DispatchType (type);

				if (type.HasFields && HasSubSteps (Categorized.on_fields)) {
					foreach (FieldDefinition field in type.Fields)
						DispatchField (field);
				}

				if (type.HasMethods && HasSubSteps (Categorized.on_methods)) {
					foreach (MethodDefinition method in type.Methods)
						DispatchMethod (method);
				}

				if (type.HasProperties && HasSubSteps (Categorized.on_properties)) {
					foreach (PropertyDefinition property in type.Properties)
						DispatchProperty (property);
				}

				if (type.HasEvents && HasSubSteps (Categorized.on_events)) {
					foreach (EventDefinition @event in type.Events)
						DispatchEvent (@event);
				}

				if (type.HasNestedTypes)
					BrowseTypes (type.NestedTypes);
			}
		}

		private void DispatchAssembly (AssemblyDefinition assembly)
		{
			foreach (var substep in Categorized.on_assemblies) {
				substep.ProcessAssembly (assembly);
			}
		}

		private void DispatchType (TypeDefinition type)
		{
			foreach (var substep in Categorized.on_types) {
				substep.ProcessType (type);
			}
		}

		private void DispatchField (FieldDefinition field)
		{
			foreach (var substep in Categorized.on_fields) {
				substep.ProcessField (field);
			}
		}

		private void DispatchMethod (MethodDefinition method)
		{
			foreach (var substep in Categorized.on_methods) {
				substep.ProcessMethod (method);
			}
		}

		private void DispatchProperty (PropertyDefinition property)
		{
			foreach (var substep in Categorized.on_properties) {
				substep.ProcessProperty (property);
			}
		}

		private void DispatchEvent (EventDefinition @event)
		{
			foreach (var substep in Categorized.on_events) {
				substep.ProcessEvent (@event);
			}
		}

		private void InitializeSubSteps (LinkContext context)
		{
			foreach (var substep in substeps)
				substep.Initialize (context);
		}

		private void CategorizeSubSteps (AssemblyDefinition assembly)
		{
			categorized = new CategorizedSubSteps {
				on_assemblies = new List<ISubStep> (),
				on_types = new List<ISubStep> (),
				on_fields = new List<ISubStep> (),
				on_methods = new List<ISubStep> (),
				on_properties = new List<ISubStep> (),
				on_events = new List<ISubStep> ()
			};

			foreach (var substep in substeps)
				CategorizeSubStep (substep, assembly);
		}

		private void CategorizeSubStep (ISubStep substep, AssemblyDefinition assembly)
		{
			if (!substep.IsActiveFor (assembly))
				return;

			CategorizeTarget (substep, SubStepTargets.Assembly, Categorized.on_assemblies);
			CategorizeTarget (substep, SubStepTargets.Type, Categorized.on_types);
			CategorizeTarget (substep, SubStepTargets.Field, Categorized.on_fields);
			CategorizeTarget (substep, SubStepTargets.Method, Categorized.on_methods);
			CategorizeTarget (substep, SubStepTargets.Property, Categorized.on_properties);
			CategorizeTarget (substep, SubStepTargets.Event, Categorized.on_events);
		}

		private static void CategorizeTarget (ISubStep substep, SubStepTargets target, List<ISubStep> list)
		{
			if (!Targets (substep, target))
				return;

			list.Add (substep);
		}

		private static bool Targets (ISubStep substep, SubStepTargets target) => (substep.Targets & target) == target;
	}
}
