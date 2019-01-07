using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps {
	public static class BaseMarkingUtils {
		public static bool ShouldMarkTypeHierarchyForMethod (LinkContext context, MethodReference method, TypeDefinition visibilityScope)
		{
			if (!NeedToCheckTypeHierarchy (context, visibilityScope))
				return false;

			if (IsTypeHierarchyRequiredForMethod (context, method, visibilityScope))
				return true;

			return false;
		}
		
		public static bool ShouldMarkTypeHierarchyForField (LinkContext context, FieldReference field, TypeDefinition visibilityScope)
		{
			if (!NeedToCheckTypeHierarchy (context, visibilityScope))
				return false;

			if (IsTypeHierarchyRequiredForField (context, field, visibilityScope))
				return true;

			return false;
		}

		static bool NeedToCheckTypeHierarchy (LinkContext context, TypeDefinition visibilityScope)
		{
			// We do not currently change the base type of value types
			if (!visibilityScope.IsClass)
				return false;

			var basesOfScope = context.Annotations.GetBaseHierarchy (visibilityScope);

			// No need to do this for types derived from object.  It already has the lowest base class
			if (basesOfScope == null || basesOfScope.Count == 0)
				return false;

			return true;
		}

		static bool IsTypeHierarchyRequiredForField (LinkContext context, FieldReference field, TypeDefinition visibilityScope)
		{
			var resolved = field.Resolve ();
			if (resolved == null) {
				// Play it safe if we fail to resolve
				return true;
			}

			var basesOfScope = context.Annotations.GetBaseHierarchy (visibilityScope);
			var fromBase = basesOfScope.FirstOrDefault (b => resolved.DeclaringType == b);
			if (fromBase != null) {
				if (!resolved.IsStatic)
					return true;

				if (resolved.IsPublic)
					return false;

				// protected
				if (resolved.IsFamily)
					return true;

				// It must be internal.  Trust that if the compiler allowed it we can continue to access
				if (!resolved.IsPrivate)
					return false;
				
				return false;
			}

			return false;
		}
		
		static bool IsTypeHierarchyRequiredForMethod (LinkContext context, MethodReference method, TypeDefinition visibilityScope)
		{
			var resolved = method.Resolve ();
			if (resolved == null) {
				// Play it safe if we fail to resolve
				return true;
			}

			var basesOfScope = context.Annotations.GetBaseHierarchy (visibilityScope);
			var fromBase = basesOfScope.FirstOrDefault (b => resolved.DeclaringType == b);
			if (fromBase != null) {
				if (!resolved.IsStatic)
					return true;

				if (resolved.IsPublic)
					return false;

				// protected
				if (resolved.IsFamily)
					return true;

				// It must be internal.  Trust that if the compiler allowed it we can continue to access
				if (!resolved.IsPrivate)
					return false;
				
				return false;
			}

			return false;
		}
	}
}