// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class ISymbolExtensions
	{
		/// <summary>
		/// Returns true if symbol <see paramref="symbol"/> has an attribute with name <see paramref="attributeName"/>.
		/// </summary>
		internal static bool HasAttribute (this ISymbol symbol, string attributeName)
		{
			foreach (var attr in symbol.GetAttributes ())
				if (attr.AttributeClass?.Name == attributeName)
					return true;

			return false;
		}

		internal static bool TryGetOverriddenMember (this ISymbol? symbol, out ISymbol? overridenMember)
		{
			overridenMember = symbol switch {
				IMethodSymbol method => method.OverriddenMethod,
				IPropertySymbol property => property.OverriddenProperty,
				IEventSymbol @event => @event.OverriddenEvent,
				_ => null,
			};
			return overridenMember != null;
		}

		internal static bool TryGetExplicitOrImplicitInterfaceImplementations (this ISymbol symbol, out ImmutableArray<ISymbol> interfaces)
		{
			if (symbol.Kind != SymbolKind.Method && symbol.Kind != SymbolKind.Property && symbol.Kind != SymbolKind.Event)
				return false;

			var containingType = symbol.ContainingType;
			var query = from iface in containingType.AllInterfaces
						from interfaceMember in iface.GetMembers ()
						let impl = containingType.FindImplementationForInterfaceMember (interfaceMember)
						where SymbolEqualityComparer.Default.Equals (symbol, impl)
						select interfaceMember;
			interfaces = query.ToImmutableArray ();
			return !interfaces.IsEmpty;
		}
	}
}
