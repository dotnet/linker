// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class ISymbolExtensions
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

		internal static bool TryGetDynamicallyAccessedMemberTypes (this ISymbol symbol, out DynamicallyAccessedMemberTypes? dynamicallyAccessedMemberTypes)
		{
			dynamicallyAccessedMemberTypes = null;
			if (!symbol.HasAttribute (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute))
				return false;

			var damAttributeName = DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute;
			AttributeData? dynamicallyAccessedMembers = null;
			foreach (var _attribute in symbol.GetAttributes ())
				if (_attribute.AttributeClass is var attrClass && attrClass != null &&
					attrClass.HasName (damAttributeName)) {
					dynamicallyAccessedMembers = _attribute;
					break;
				}

			dynamicallyAccessedMemberTypes = (DynamicallyAccessedMemberTypes) dynamicallyAccessedMembers?.ConstructorArguments[0].Value!;
			return dynamicallyAccessedMemberTypes != null;
		}

		internal static bool TryGetDynamicallyAccessedMemberTypesOnReturnType (this ISymbol symbol, out DynamicallyAccessedMemberTypes? dynamicallyAccessedMemberTypes)
		{
			dynamicallyAccessedMemberTypes = null;
			if (symbol is not IMethodSymbol methodSymbol)
				return false;

			AttributeData? dynamicallyAccessedMembers = null;
			foreach (var returnTypeAttribute in methodSymbol.GetReturnTypeAttributes ())
				if (returnTypeAttribute.AttributeClass is var attrClass && attrClass != null &&
					attrClass.HasName (DynamicallyAccessedMembersAnalyzer.DynamicallyAccessedMembersAttribute)) {
					dynamicallyAccessedMembers = returnTypeAttribute;
					break;
				}

			if (dynamicallyAccessedMembers == null)
				return false;

			dynamicallyAccessedMemberTypes = (DynamicallyAccessedMemberTypes) dynamicallyAccessedMembers.ConstructorArguments[0].Value!;
			return true;
		}

		internal static bool TryGetOverriddenMember (this ISymbol? symbol, [NotNullWhen (returnValue: true)] out ISymbol? overridenMember)
		{
			overridenMember = symbol switch {
				IMethodSymbol method => method.OverriddenMethod,
				IPropertySymbol property => property.OverriddenProperty,
				IEventSymbol @event => @event.OverriddenEvent,
				_ => null,
			};
			return overridenMember != null;
		}

		public static string GetDisplayName (this ISymbol symbol)
		{
			var sb = new StringBuilder ();
			switch (symbol) {
			case IFieldSymbol fieldSymbol:
				sb.Append (fieldSymbol.Type);
				sb.Append (" ");
				sb.Append (fieldSymbol.ContainingSymbol.ToDisplayString ());
				sb.Append ("::");
				sb.Append (fieldSymbol.MetadataName);
				break;

			case IParameterSymbol parameterSymbol:
				sb.Append (parameterSymbol.Name);
				break;

			default:
				sb.Append (symbol.ToDisplayString ());
				break;
			}

			return sb.ToString ();
		}
	}
}
