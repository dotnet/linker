// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	public readonly struct ReflectionMarker
	{
		readonly LinkContext _context;
		readonly MarkStep _markStep;

		public ReflectionMarker (LinkContext context, MarkStep markStep)
		{
			_context = context;
			_markStep = markStep;
		}

		internal void MarkTypeForDynamicallyAccessedMembers (in MessageOrigin origin, TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes requiredMemberTypes, DependencyKind dependencyKind, bool declaredOnly = false)
		{
			foreach (var member in typeDefinition.GetDynamicallyAccessedMembers (_context, requiredMemberTypes, declaredOnly)) {
				switch (member) {
				case MethodDefinition method:
					MarkMethod (origin, method, dependencyKind);
					break;
				case FieldDefinition field:
					MarkField (origin, field, dependencyKind);
					break;
				case TypeDefinition nestedType:
					MarkType (origin, nestedType, dependencyKind);
					break;
				case PropertyDefinition property:
					MarkProperty (origin, property, dependencyKind);
					break;
				case EventDefinition @event:
					MarkEvent (origin, @event, dependencyKind);
					break;
				case InterfaceImplementation interfaceImplementation:
					MarkInterfaceImplementation (origin, interfaceImplementation, dependencyKind);
					break;
				}
			}
		}

		internal void MarkType (in MessageOrigin origin, TypeReference typeReference, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			if (_context.TryResolve (typeReference) is TypeDefinition type)
				_markStep.MarkTypeVisibleToReflection (typeReference, type, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkMethod (in MessageOrigin origin, MethodDefinition method, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkMethodVisibleToReflection (method, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkField (in MessageOrigin origin, FieldDefinition field, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkFieldVisibleToReflection (field, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		internal void MarkProperty (in MessageOrigin origin, PropertyDefinition property, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkPropertyVisibleToReflection (property, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkEvent (in MessageOrigin origin, EventDefinition @event, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkEventVisibleToReflection (@event, new DependencyInfo (dependencyKind, origin.Provider), origin);
		}

		void MarkInterfaceImplementation (in MessageOrigin origin, InterfaceImplementation interfaceImplementation, DependencyKind dependencyKind = DependencyKind.AccessedViaReflection)
		{
			_markStep.MarkInterfaceImplementation (interfaceImplementation, null, new DependencyInfo (dependencyKind, origin.Provider));
		}

		internal void MarkConstructorsOnType (in MessageOrigin origin, TypeDefinition type, Func<MethodDefinition, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var ctor in type.GetConstructorsOnType (filter, bindingFlags))
				MarkMethod (origin, ctor);
		}

		internal void MarkFieldsOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var field in type.GetFieldsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkField (origin, field);
		}

		internal void MarkPropertiesOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var property in type.GetPropertiesOnTypeHierarchy (_context, filter, bindingFlags))
				MarkProperty (origin, property);
		}

		internal void MarkEventsOnTypeHierarchy (in MessageOrigin origin, TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			foreach (var @event in type.GetEventsOnTypeHierarchy (_context, filter, bindingFlags))
				MarkEvent (origin, @event);
		}

		internal void MarkStaticConstructor (in MessageOrigin origin, TypeDefinition type)
		{
			_markStep.MarkStaticConstructorVisibleToReflection (type, new DependencyInfo (DependencyKind.AccessedViaReflection, origin.Provider), origin);
		}
	}
}