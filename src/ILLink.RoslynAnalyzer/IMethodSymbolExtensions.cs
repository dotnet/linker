// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class IMethodSymbolExtensions
	{
		internal static bool TryGetReturnTypeAttribute(this IMethodSymbol methodSymbol, string attributeName, [NotNullWhen(true)] out AttributeData? attribute)
		{
			attribute = null;
			if (methodSymbol.GetReturnTypeAttributes ().FirstOrDefault (a => a.AttributeClass?.Name == attributeName) is AttributeData tmpAttribute) {
				attribute = tmpAttribute;
				return true;
			} else {
				attribute = null;
				return false;
			}
		}
	}
}
