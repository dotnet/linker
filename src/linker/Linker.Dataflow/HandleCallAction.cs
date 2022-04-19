// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Reflection;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct HandleCallAction
	{
#pragma warning disable CA1822 // Mark members as static - the other partial implementations might need to be instance methods

		readonly LinkContext _context;
		readonly ReflectionMarker _reflectionMarker;
		readonly MessageOrigin _origin;
		readonly MethodDefinition _callingMethodDefinition;

		public HandleCallAction (
			LinkContext context,
			ReflectionMarker reflectionMarker,
			in DiagnosticContext diagnosticContext,
			MethodDefinition callingMethodDefinition)
		{
			_context = context;
			_reflectionMarker = reflectionMarker;
			_diagnosticContext = diagnosticContext;
			_origin = diagnosticContext.Origin;
			_callingMethodDefinition = callingMethodDefinition;
			_annotations = context.Annotations.FlowAnnotations;
			_requireDynamicallyAccessedMembersAction = new (context, reflectionMarker, diagnosticContext);
		}

		private partial bool MethodIsTypeConstructor (MethodProxy method)
		{
			if (!method.Method.IsConstructor)
				return false;
			TypeDefinition? type = method.Method.DeclaringType;
			while (type is not null) {
				if (type.IsTypeOf (WellKnownType.System_Type))
					return true;
				type = _context.Resolve (type.BaseType);
			}
			return false;
		}

		private partial IEnumerable<SystemReflectionMethodBaseValue> GetMethodsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var method in type.Type.GetMethodsOnTypeHierarchy (_context, m => m.Name == name, bindingFlags))
				yield return new SystemReflectionMethodBaseValue (new MethodProxy (method));
		}

		private partial IEnumerable<SystemTypeValue> GetNestedTypesOnType (TypeProxy type, string name, BindingFlags? bindingFlags)
		{
			foreach (var nestedType in type.Type.GetNestedTypesOnType (t => t.Name == name, bindingFlags))
				yield return new SystemTypeValue (new TypeProxy (nestedType));
		}

		private partial bool TryGetBaseType (TypeProxy type, out TypeProxy? baseType)
		{
			if (type.Type.BaseType is TypeReference baseTypeRef && _context.TryResolve (baseTypeRef) is TypeDefinition baseTypeDefinition) {
				baseType = new TypeProxy (baseTypeDefinition);
				return true;
			}

			baseType = null;
			return false;
		}

		private partial void MarkStaticConstructor (TypeProxy type)
			=> _reflectionMarker.MarkStaticConstructor (_origin, type.Type);

		private partial void MarkEventsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkEventsOnTypeHierarchy (_origin, type.Type, e => e.Name == name, bindingFlags);

		private partial void MarkFieldsOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkFieldsOnTypeHierarchy (_origin, type.Type, f => f.Name == name, bindingFlags);

		private partial void MarkPropertiesOnTypeHierarchy (TypeProxy type, string name, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkPropertiesOnTypeHierarchy (_origin, type.Type, p => p.Name == name, bindingFlags);

		private partial void MarkPublicParameterlessConstructorOnType (TypeProxy type)
			=> _reflectionMarker.MarkConstructorsOnType (_origin, type.Type, m => m.IsPublic && m.Parameters.Count == 0);

		private partial void MarkConstructorsOnType (TypeProxy type, BindingFlags? bindingFlags)
			=> _reflectionMarker.MarkConstructorsOnType (_origin, type.Type, null, bindingFlags);

		private partial void MarkMethod (MethodProxy method)
			=> _reflectionMarker.MarkMethod (_origin, method.Method);

		private partial void MarkType (TypeProxy type)
			=> _reflectionMarker.MarkType (_origin, type.Type);

		private partial bool MarkAssociatedProperty (MethodProxy method)
		{
			if (method.Method.TryGetProperty (out PropertyDefinition? propertyDefinition)) {
				_reflectionMarker.MarkProperty (_origin, propertyDefinition);
				return true;
			}

			return false;
		}

		private partial string GetContainingSymbolDisplayName () => _callingMethodDefinition.GetDisplayName ();
	}
}
