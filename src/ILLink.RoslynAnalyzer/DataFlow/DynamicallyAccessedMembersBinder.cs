// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.DataFlow
{
	internal static class DynamicallyAccessedMembersBinder
	{
		// Temporary workaround - should be removed once linker can be upgraded to build against
		// high enough version of the framework which has this enum value.
		internal static class DynamicallyAccessedMemberTypesOverlay
		{
			public const DynamicallyAccessedMemberTypes Interfaces = (DynamicallyAccessedMemberTypes) 0x2000;
		}

		// Returns the members of the type bound by memberTypes. For DynamicallyAccessedMemberTypes.All, this returns all members of the type and its
		// nested types, including interface implementations, plus the same or any base types or implemented interfaces.
		// DynamicallyAccessedMemberTypes.PublicNestedTypes and NonPublicNestedTypes do the same for members of the selected nested types.
		public static IEnumerable<ISymbol> GetDynamicallyAccessedMembers (this ITypeSymbol typeDefinition, OperationAnalysisContext context, DynamicallyAccessedMemberTypes memberTypes, bool declaredOnly = false)
		{
			if (memberTypes == DynamicallyAccessedMemberTypes.None)
				yield break;

			if (memberTypes == DynamicallyAccessedMemberTypes.All) {
				var members = new List<ISymbol> ();
				typeDefinition.GetAllOnType (context, declaredOnly, members);
				foreach (var m in members)
					yield return m;
				yield break;
			}

			var declaredOnlyFlags = declaredOnly ? BindingFlags.DeclaredOnly : BindingFlags.Default;

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicConstructors)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: null, bindingFlags: BindingFlags.NonPublic))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicConstructors)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: null, bindingFlags: BindingFlags.Public))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)) {
				foreach (var c in typeDefinition.GetConstructorsOnType (filter: m => (m.DeclaredAccessibility== Accessibility.Public) && m.Parameters.Length == 0))
					yield return c;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicMethods)) {
				foreach (var m in typeDefinition.GetMethodsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return m;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicFields)) {
				foreach (var f in typeDefinition.GetFieldsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return f;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.NonPublic)) {
					yield return nested;
					var members = new List<ISymbol> ();
					nested.GetAllOnType (context, declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicNestedTypes)) {
				foreach (var nested in typeDefinition.GetNestedTypesOnType (filter: null, bindingFlags: BindingFlags.Public)) {
					yield return nested;
					var members = new List<ISymbol> ();
					nested.GetAllOnType (context, declaredOnly: false, members);
					foreach (var m in members)
						yield return m;
				}
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicProperties)) {
				foreach (var p in typeDefinition.GetPropertiesOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return p;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.NonPublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.NonPublic | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypes.PublicEvents)) {
				foreach (var e in typeDefinition.GetEventsOnTypeHierarchy (context, filter: null, bindingFlags: BindingFlags.Public | declaredOnlyFlags))
					yield return e;
			}

			if (memberTypes.HasFlag (DynamicallyAccessedMemberTypesOverlay.Interfaces)) {
				foreach (var i in typeDefinition.GetAllInterfaceImplementations (context, declaredOnly))
					yield return i;
			}
		}
		public static IEnumerable<IMethodSymbol> GetConstructorsOnType (this ITypeSymbol type, Func<IMethodSymbol, bool>? filter, BindingFlags? bindingFlags = null)
		{
			foreach (var member in type.GetMembers () ) {
				if (!(member is IMethodSymbol method))
					continue;
				if (method.MethodKind != MethodKind.Constructor)
					continue;

				if (filter != null && !filter (method))
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && method.DeclaredAccessibility != Accessibility.Public)
					continue;

				if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.DeclaredAccessibility == Accessibility.Public)
					continue;

				yield return method;
			}
		}

		public static IEnumerable<IMethodSymbol> GetMethodsOnTypeHierarchy (this ITypeSymbol thisType, OperationAnalysisContext context, Func<IMethodSymbol, bool>? filter, BindingFlags? bindingFlags = null)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var member in type.GetMembers ()) {
					if (!(member is IMethodSymbol method))
						continue;
					// Ignore constructors as those are not considered methods from a reflection's point of view
					if (method.MethodKind == MethodKind.Constructor)
						continue;

					// Ignore private methods on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && method.DeclaredAccessibility == Accessibility.Private)
						continue;

					// Note that special methods like property getter/setter, event adder/remover will still get through and will be marked.
					// This is intentional as reflection treats these as methods as well.

					if (filter != null && !filter (method))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && method.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && method.DeclaredAccessibility != Accessibility.Public)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && method.DeclaredAccessibility == Accessibility.Public)
						continue;

					yield return method;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<IFieldSymbol> GetFieldsOnTypeHierarchy (this ITypeSymbol thisType, OperationAnalysisContext context, Func<IFieldSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var member in type.GetMembers ()) {
					if (!(member is IFieldSymbol field))
						continue;
					// Ignore private fields on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					if (onBaseType && field.DeclaredAccessibility == Accessibility.Private)
						continue;

					// Note that compiler generated fields backing some properties and events will get through here.
					// This is intentional as reflection treats these as fields as well.

					if (filter != null && !filter (field))
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Static && !field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Instance | BindingFlags.Static)) == BindingFlags.Instance && field.IsStatic)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public && field.DeclaredAccessibility != Accessibility.Public)
						continue;

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic && field.DeclaredAccessibility == Accessibility.Public)
						continue;

					yield return field;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<ITypeSymbol> GetNestedTypesOnType (this ITypeSymbol type, Func<ITypeSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			// @TODO
			throw new NotImplementedException ("@TODO");
			//type.InheritsFromOrEquals()
			//foreach (var nestedType in type.NestedTypes) {
			//	ITypeSymbol
			//	if (filter != null && !filter (nestedType))
			//		continue;

			//	if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.Public) {
			//		if (!nestedType.IsNestedPublic)
			//			continue;
			//	}

			//	if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
			//		if (nestedType.IsNestedPublic)
			//			continue;
			//	}

			//	yield return nestedType;
		}

		public static IEnumerable<IPropertySymbol> GetPropertiesOnTypeHierarchy (this ITypeSymbol thisType, OperationAnalysisContext context, Func<IPropertySymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var member in type.GetMembers ()) {
					if (!(member is IPropertySymbol property))
						continue;
					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(property.GetMethod == null || property.GetMethod.DeclaredAccessibility == Accessibility.Private) &&
						(property.SetMethod == null || property.SetMethod.DeclaredAccessibility == Accessibility.Private))
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
						if ((property.GetMethod == null || (property.GetMethod.DeclaredAccessibility != Accessibility.Public))
							&& (property.SetMethod == null || (property.SetMethod.DeclaredAccessibility != Accessibility.Public)))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((property.GetMethod != null) && (property.GetMethod.DeclaredAccessibility == Accessibility.Public)) continue;
						if ((property.SetMethod != null) && (property.SetMethod.DeclaredAccessibility == Accessibility.Public)) continue;
					}

					yield return property;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		public static IEnumerable<IEventSymbol> GetEventsOnTypeHierarchy (this ITypeSymbol thisType, OperationAnalysisContext context, Func<IEventSymbol, bool>? filter, BindingFlags? bindingFlags = BindingFlags.Default)
		{
			ITypeSymbol? type = thisType;
			bool onBaseType = false;
			while (type != null) {
				foreach (var member in type.GetMembers ()) {
					if (!(member is IEventSymbol @event))
						continue;

					// Ignore private properties on a base type - those are completely ignored by reflection
					// (anything private on the base type is not visible via the derived type)
					// Note that properties themselves are not actually private, their accessors are
					if (onBaseType &&
						(@event.AddMethod == null || @event.AddMethod.DeclaredAccessibility == Accessibility.Private) &&
						(@event.RemoveMethod == null || @event.RemoveMethod.DeclaredAccessibility == Accessibility.Private))
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
						if ((@event.AddMethod == null || (@event.AddMethod.DeclaredAccessibility != Accessibility.Public))
							&& (@event.RemoveMethod == null || (@event.RemoveMethod.DeclaredAccessibility != Accessibility.Public)))
							continue;
					}

					if ((bindingFlags & (BindingFlags.Public | BindingFlags.NonPublic)) == BindingFlags.NonPublic) {
						if ((@event.AddMethod != null) && @event.AddMethod.DeclaredAccessibility == Accessibility.Public) continue;
						if ((@event.RemoveMethod != null) && @event.RemoveMethod.DeclaredAccessibility == Accessibility.Public) continue;
					}

					yield return @event;
				}

				if ((bindingFlags & BindingFlags.DeclaredOnly) == BindingFlags.DeclaredOnly)
					yield break;

				type = type.BaseType;
				onBaseType = true;
			}
		}

		// declaredOnly will cause this to retrieve interfaces recursively required by the type, but doesn't necessarily
		// include interfaces required by any base types.
		public static IEnumerable<ITypeSymbol> GetAllInterfaceImplementations (this ITypeSymbol thisType, OperationAnalysisContext context, bool declaredOnly)
		{
			ITypeSymbol? type = thisType;
			while (type != null) {
				foreach (var i in type.Interfaces) {
					yield return i;

					ITypeSymbol? interfaceType = i;
					if (interfaceType != null) {
						// declaredOnly here doesn't matter since interfaces don't have base types
						foreach (var innerInterface in interfaceType.GetAllInterfaceImplementations (context, declaredOnly: true))
							yield return innerInterface;
					}
				}

				if (declaredOnly)
					yield break;

				type = type.BaseType;
			}
		}

		// declaredOnly will cause this to retrieve only members of the type, not of its base types. This includes interfaces recursively
		// required by this type (but not members of these interfaces, or interfaces required only by base types).
		public static void GetAllOnType (this ITypeSymbol type, OperationAnalysisContext context, bool declaredOnly, List<ISymbol> members) => GetAllOnType (type, context, declaredOnly, members, new HashSet<ITypeSymbol> ());

		static void GetAllOnType (ITypeSymbol type, OperationAnalysisContext context, bool declaredOnly, List<ISymbol> members, HashSet<ITypeSymbol> types)
		{
			if (!types.Add (type))
				return;

			// @TODO
			//if (type.HasNestedTypes) {
			//	foreach (var nested in type.NestedTypes) {
			//		members.Add (nested);
			//		// Base types and interfaces of nested types are always included.
			//		GetAllOnType (nested, context, declaredOnly: false, members, types);
			//	}
			//}

			if (!declaredOnly) {
				var baseType = type.BaseType;
				if (baseType != null)
					GetAllOnType (baseType, context, declaredOnly: false, members, types);
			}

			if (!type.Interfaces.IsEmpty) {
				if (declaredOnly) {
					foreach (var iface in type.GetAllInterfaceImplementations (context, declaredOnly: true))
						members.Add (iface);
				} else {
					foreach (var iface in type.Interfaces) {
						members.Add (iface);
						var interfaceType = iface;
						if (interfaceType == null)
							continue;
						GetAllOnType (interfaceType, context, declaredOnly: false, members, types);
					}
				}
			}

			foreach (var member in type.GetMembers ()) {
				if (member is IFieldSymbol f)
					members.Add (f);
				if (member is IMethodSymbol m)
					members.Add (m);
				if (member is IPropertySymbol p)
					members.Add (p);
				if (member is IEventSymbol e)
					members.Add (e);
			}
		}


	}
}
