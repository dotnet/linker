// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class RequiresISymbolExtensions
	{
		// TODO: Consider sharing with linker DoesMethodRequireUnreferencedCode method
		/// <summary>
		/// True if the target of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool TargetHasRequiresAttribute (this ISymbol member, string requiresAttribute, [NotNullWhen (returnValue: true)] out AttributeData? requiresAttributeData)
		{
			requiresAttributeData = null;
			if (member.IsStaticConstructor ())
				return false;

			if (member.TryGetAttribute (requiresAttribute, out requiresAttributeData))
				return true;

			// Also check the containing type
			if (member.IsStatic || member.IsConstructor ())
				return member.ContainingType.TryGetAttribute (requiresAttribute, out requiresAttributeData);

			return false;
		}

		// TODO: Consider sharing with linker IsMethodInRequiresUnreferencedCodeScope method
		/// <summary>
		/// True if the source of a call is considered to be annotated with the Requires... attribute
		/// </summary>
		public static bool IsInRequiresScope (this ISymbol member, string requiresAttribute)
		{
			return member.IsInRequiresScope (requiresAttribute, true);
		}

		/// <summary>
		/// True if member of a call is considered to be annotated with the Requires... attribute.
		/// Doesn't check the associated symbol for overrides and virtual methods because the analyzer should warn on mismatched between the property AND the accessors
		/// </summary>
		/// <param name="containingSymbol">
		///	Symbol that is either an overriding member or an overriden/virtual member
		/// </param>
		public static bool IsOverrideInRequiresScope (this ISymbol member, string requiresAttribute)
		{
			return member.IsInRequiresScope (requiresAttribute, false);
		}

		private static bool IsInRequiresScope (this ISymbol symbol, string requiresAttribute, bool checkAssociatedSymbol)
		{
			// Requires attribute on a type does not silence warnings that originate
			// from the type directly. We also only check the containing type for members
			// below, not of nested types.
			if (symbol is ITypeSymbol)
				return false;

			if (symbol.HasAttribute (requiresAttribute)
				|| (symbol.ContainingType is ITypeSymbol containingType &&
					containingType.HasAttribute (requiresAttribute))) {
				return true;
			}
			// Only check associated symbol if not override or virtual method
			if (checkAssociatedSymbol && symbol is IMethodSymbol { AssociatedSymbol: { } associated } && associated.HasAttribute (requiresAttribute))
				return true;

			return false;
		}
	}
}
