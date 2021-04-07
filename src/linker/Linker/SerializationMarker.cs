// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;
using Mono.Linker.Dataflow;
using Mono.Linker.Steps;

namespace Mono.Linker
{

	// This recursively marks members of types for serialization. This is not supposed to be complete; it is just
	// here for back-compat with the old behavior of xamarin-android.
	//
	// The xamarin-android behavior was as follows:
	//
	// Discover members in the "link" assemblies with certain attributes:
	//   for XMLSerializer: Xml*Attribute, except XmlIgnoreAttribute
	//   for DataContractSerializer: DataContractAttribute or DataMemberAttribute
	// These members are considered "roots" for serialization.
	//
	// For each "root":
	//    in an SDK assembly, set TypePreserve.All for types, or conditionally preserve methods or property methods
	//      event methods were not preserved.
	//    in a non-SDK assembly, mark types, fields, methods, property methods, and event methods
	//    recursively scan types of properties and fields (including generic arguments)
	//      for each recursive type, conditionally preserve the default ctor
	//
	// We want to match the above behavior in a more consistent way (erring on the side of being more conservative).

	// Instead of conditionally preserving things, we will just mark them, and we will do so consistently for every
	// type discovered as part of the type graph reachable from the discovered roots. We also do not distinguish between
	// SDK and non-SDK assemblies.
	//
	// The behavior is as follows:
	//
	// Discover attributed "roots" the same way.
	//
	// For each "root":
	//   recursively scan types of properties and fields (including generic arguments)
	//     for each recursive type:
	//       mark the type, and set TypePreserve.All
	//         this handles the cases where XA used to set TypePreserve.All, preserve default ctors, and mark or conditionally preserve fields/methods (including property and event methods)

	public class SerializationMarker
	{

		readonly LinkContext _context;

		public SerializationMarker (LinkContext context)
		{
			_context = context;
		}

		public void MarkRecursiveMembers (TypeReference typeRef, in DependencyInfo reason)
		{
			MarkRecursiveMembersInternal (typeRef, reason);
		}

		HashSet<TypeDefinition> _recursiveTypes;
		HashSet<TypeDefinition> RecursiveTypes {
			get {
				if (_recursiveTypes == null)
					_recursiveTypes = new HashSet<TypeDefinition> ();

				return _recursiveTypes;
			}
		}

		void MarkRecursiveMembersInternal (TypeReference typeRef, in DependencyInfo reason)
		{
			if (typeRef == null)
				return;

			DependencyInfo typeReason = reason;
			while (typeRef is GenericInstanceType git) {
				if (git.HasGenericArguments) {
					foreach (var argType in git.GenericArguments)
						MarkRecursiveMembersInternal (argType, new DependencyInfo (DependencyKind.GenericArgumentType, typeRef));
				}
				_context.Tracer.AddDirectDependency (typeRef, typeReason, marked: false);
				typeReason = new DependencyInfo (DependencyKind.ElementType, typeRef);
				typeRef = (typeRef as TypeSpecification).ElementType;
			}
			// This doesn't handle other TypeSpecs. We are only matching what xamarin-android used to do.

			TypeDefinition type = typeRef.Resolve ();
			if (type == null)
				return;

			_context.Annotations.Mark (type, typeReason);

			if (!RecursiveTypes.Add (type))
				return;

			// Quirk to match xamarin-android behavior, which would set TypePreserve.All
			// for discovered root types in "link" SDK assemblies. We do it for all recursive
			// types for consistency.
			_context.Annotations.SetPreserve (type, TypePreserve.All);

			MarkRecursiveMembersInternal (type.BaseType, new DependencyInfo (DependencyKind.BaseType, type));

			if (type.HasFields) {
				foreach (var field in type.Fields) {
					// Static field types are discovered, matching xamarin-android behavior
					MarkRecursiveMembersInternal (field.FieldType, new DependencyInfo (DependencyKind.RecursiveType, type));
					// marking the field is handled by TypePreserve.All
				}
			}

			if (type.HasProperties) {
				foreach (var property in type.Properties) {
					// Static property types are discovered, matching xamarin-android behavior
					MarkRecursiveMembersInternal (property.PropertyType, new DependencyInfo (DependencyKind.RecursiveType, type));
					// getter/setter are handled by TypePreserve.All
				}
			}

			// Default ctors and other methods are handled by TypePreserve.All
		}
	}
}