// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;
using Mono.Linker;
using Mono.Linker.Dataflow;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct RequireDynamicallyAccessedMembersAction
	{
		readonly LinkContext _context;
		readonly ReflectionMarker _reflectionMarker;

		public RequireDynamicallyAccessedMembersAction (
			LinkContext context,
			ReflectionMarker reflectionMarker,
			in DiagnosticContext diagnosticContext)
		{
			_context = context;
			_reflectionMarker = reflectionMarker;
			_diagnosticContext = diagnosticContext;
		}

		private partial bool TryResolveTypeNameAndMark (string typeName, out TypeProxy type)
		{
			if (!_context.TypeNameResolver.TryResolveTypeName (typeName, _diagnosticContext.Origin.Provider, out TypeReference? typeRef, out AssemblyDefinition? typeAssembly)
				|| typeRef.ResolveToTypeDefinition (_context) is not TypeDefinition foundType) {
				type = default;
				return false;
			} else {
				_reflectionMarker.MarkType (_diagnosticContext.Origin, typeRef);
				_context.MarkingHelpers.MarkMatchingExportedType (foundType, typeAssembly, new DependencyInfo (DependencyKind.DynamicallyAccessedMember, foundType), _diagnosticContext.Origin);
				type = new TypeProxy (foundType);
				return true;
			}
		}

		private partial void MarkTypeForDynamicallyAccessedMembers (in TypeProxy type, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			_reflectionMarker.MarkTypeForDynamicallyAccessedMembers (_diagnosticContext.Origin, type.Type, dynamicallyAccessedMemberTypes, DependencyKind.DynamicallyAccessedMember);
		}
	}
}
