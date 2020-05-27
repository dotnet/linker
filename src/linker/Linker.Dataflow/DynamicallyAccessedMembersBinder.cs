using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Mono.Cecil;

namespace Mono.Linker.Dataflow
{
	internal static class DynamicallyAccessedMembersBinder
	{
		// Returns the members of the type bound by memberTypes. For MemberTypes.All, this returns a single null result.
		public static IEnumerable<IMemberDefinition> GetDynamicallyAccessedMembers (TypeDefinition typeDefinition, DynamicallyAccessedMemberTypes memberTypes)
		{
			if (memberTypes == DynamicallyAccessedMemberTypes.All) {
				yield return null;
				yield break;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicConstructors)) {
				foreach (var c in GetConstructorsOnType (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
				foreach (var c in GetConstructorsOnType (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.DefaultConstructor)) {
				foreach (var c in GetConstructorsOnType (typeDefinition, filter: m => m.IsPublic && m.Parameters.Count == 0))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicMethods)) {
				foreach (var m in GetMethodsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods)) {
				foreach (var m in GetMethodsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicFields)) {
				foreach (var f in GetFieldsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields)) {
				foreach (var f in GetFieldsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)) {
				foreach (var t in GetNestedTypesOnType (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return t;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicNestedTypes)) {
				foreach (var t in GetNestedTypesOnType (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return t;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicProperties)) {
				foreach (var p in GetPropertiesOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties)) {
				foreach (var p in GetPropertiesOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicEvents)) {
				foreach (var e in GetEventsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents)) {
				foreach (var e in GetEventsOnTypeHierarchy (typeDefinition, filter: null, bindingFlags: BindingFlags.Public))
					yield return e;
			}
		}

		public static IEnumerable<MethodDefinition> GetConstructorsOnType (TypeDefinition type, Func<MethodDefinition, bool> filter, BindingFlags? bindingFlags = null)
		{
			foreach (var method in type.Methods) {
				if (!method.IsConstructor)
					continue;

				if (filter != null && !filter (method))
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
					continue;

				yield return method;
			}
		}

		public static IEnumerable<MethodDefinition> GetMethodsOnTypeHierarchy (TypeDefinition type, Func<MethodDefinition, bool> filter, BindingFlags? bindingFlags = null)
		{
			bool onBaseType = false;
			while (type != null) {
				foreach (var method in type.Methods) {
					// Ignore constructors as those are not considered methods from a reflection's point of view
					if (method.IsConstructor)
						continue;

					// Ignore private methods on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && method.IsPrivate)
						continue;

					// Note that special methods like property getter/setter, event adder/remover will still get through and will be marked.
					// This is intentional as reflection treats these as methods as well.

					if (filter != null && !filter (method))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !method.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.IsPublic)
						continue;

					yield return method;
				}

				type = type.BaseType?.Resolve ();
				onBaseType = true;
			}
		}

		public static IEnumerable<FieldDefinition> GetFieldsOnTypeHierarchy (TypeDefinition type, Func<FieldDefinition, bool> filter, BindingFlags bindingFlags = BindingFlags.Default)
		{
			bool onBaseType = false;
			while (type != null) {
				foreach (var field in type.Fields) {
					// Ignore private fields on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && field.IsPrivate)
						continue;

					// Note that compiler generated fields backing some properties and events will get through here.
					// This is intentional as reflection treats these as fields as well.

					if (filter != null && !filter (field))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && !field.IsPublic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && field.IsPublic)
						continue;

					yield return field;
				}

				type = type.BaseType?.Resolve ();
				onBaseType = true;
			}
		}

		public static IEnumerable<TypeDefinition> GetNestedTypesOnType (TypeDefinition type, Func<TypeDefinition, bool> filter, BindingFlags bindingFlags = BindingFlags.Default)
		{
			foreach (var nestedType in type.NestedTypes) {
				if (filter != null && !filter (nestedType))
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
					if (!nestedType.IsNestedPublic)
						continue;
				}

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
					if (nestedType.IsNestedPublic)
						continue;
				}

				yield return nestedType;
			}
		}

		public static IEnumerable<PropertyDefinition> GetPropertiesOnTypeHierarchy (TypeDefinition type, Func<PropertyDefinition, bool> filter, BindingFlags bindingFlags = BindingFlags.Default)
		{
			bool onBaseType = false;
			while (type != null) {
				foreach (var property in type.Properties) {
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(property.GetMethod == null || property.GetMethod.IsPrivate) &&
						(property.SetMethod == null || property.SetMethod.IsPrivate))
						continue;

					if (filter != null && !filter (property))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static) {
						if ((property.GetMethod != null) && !property.GetMethod.IsStatic) continue;
						if ((property.SetMethod != null) && !property.SetMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance) {
						if ((property.GetMethod != null) && property.GetMethod.IsStatic) continue;
						if ((property.SetMethod != null) && property.SetMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
						if ((property.GetMethod == null || !property.GetMethod.IsPublic)
							&& (property.SetMethod == null || !property.SetMethod.IsPublic))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((property.GetMethod != null) && property.GetMethod.IsPublic) continue;
						if ((property.SetMethod != null) && property.SetMethod.IsPublic) continue;
					}

					yield return property;
				}

				type = type.BaseType?.Resolve ();
				onBaseType = true;
			}
		}

		public static IEnumerable<EventDefinition> GetEventsOnTypeHierarchy (TypeDefinition type, Func<EventDefinition, bool> filter, BindingFlags bindingFlags = BindingFlags.Default)
		{
			bool onBaseType = false;
			while (type != null) {
				foreach (var @event in type.Events) {
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(@event.AddMethod == null || @event.AddMethod.IsPrivate) &&
						(@event.RemoveMethod == null || @event.RemoveMethod.IsPrivate))
						continue;

					if (filter != null && !filter (@event))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static) {
						if ((@event.AddMethod != null) && !@event.AddMethod.IsStatic) continue;
						if ((@event.RemoveMethod != null) && !@event.RemoveMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance) {
						if ((@event.AddMethod != null) && @event.AddMethod.IsStatic) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.IsStatic) continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
						if ((@event.AddMethod == null || !@event.AddMethod.IsPublic)
							&& (@event.RemoveMethod == null || !@event.RemoveMethod.IsPublic))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((@event.AddMethod != null) && @event.AddMethod.IsPublic) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.IsPublic) continue;
					}

					yield return @event;
				}

				type = type.BaseType?.Resolve ();
				onBaseType = true;
			}
		}
	}
}