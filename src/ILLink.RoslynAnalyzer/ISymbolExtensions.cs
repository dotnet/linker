// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

		internal static bool TryGetRequiresAssemblyFileAttribute (this ISymbol symbol, out AttributeData? attribute)
		{
			attribute = null;
			if (symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass is { } attrClass &&
				attrClass.HasName (RequiresAssemblyFilesAnalyzer.FullyQualifiedRequiresAssemblyFilesAttribute)) is var _attribute &&
				_attribute != null && _attribute.ConstructorArguments.Length == 0) {
				attribute = _attribute;
				return true;
			}

			return false;
		}

		internal static bool TryGetAttributeWithMessageOnCtor (this ISymbol symbol, string qualifiedAttributeName, out AttributeData? attribute)
		{
			attribute = null;
			if (symbol.GetAttributes ().FirstOrDefault (attr => attr.AttributeClass is { } attrClass &&
				attrClass.HasName (qualifiedAttributeName)) is var _attribute &&
				_attribute != null && _attribute.ConstructorArguments.Length >= 1 &&
				_attribute.ConstructorArguments[0] is { Type: { SpecialType: SpecialType.System_String } } ctorArg) {
				attribute = _attribute;
				return true;
			}

			return false;
		}
	}
}
