// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	internal static class IParameterSymbolExtensions
	{
		public static ILParameterIndex GetILParameterIndex (this IParameterSymbol parameterSymbol)
			=> (ILParameterIndex) (((IMethodSymbol) parameterSymbol.ContainingSymbol).IsStatic ? parameterSymbol.Ordinal : parameterSymbol.Ordinal + 1);
	}
}