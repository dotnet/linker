// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ILLink.RoslynAnalyzer
{
	public static class SemanticModelExtensions
	{
		public static ISymbol? GetSymbol (this SemanticModel semanticModel, ExpressionSyntax expression)
		{
			var symbolInfo = semanticModel.GetSymbolInfo (expression);
			if (expression is TypeOfExpressionSyntax typeOfExpression)
				symbolInfo = semanticModel.GetSymbolInfo (typeOfExpression.Type);

			return symbolInfo.Symbol;
		} 
	}
}
