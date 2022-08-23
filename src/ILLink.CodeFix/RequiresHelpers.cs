﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis;
using ILLink.RoslynAnalyzer;

namespace ILLink.CodeFixProvider
{
	class RequiresHelpers
	{
		internal static SyntaxNode[] GetAttributeArgumentsForRequires (ISymbol targetSymbol, SyntaxGenerator syntaxGenerator, bool hasPublicAccessibility)
		{
			var symbolDisplayName = targetSymbol.GetDisplayName ();
			if (string.IsNullOrEmpty (symbolDisplayName) || hasPublicAccessibility)
				return Array.Empty<SyntaxNode> ();

			return new[] { syntaxGenerator.AttributeArgument (syntaxGenerator.LiteralExpression ($"Calls {symbolDisplayName}")) };
		}
	}
}
