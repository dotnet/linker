// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using static ILLink.RoslynAnalyzer.RequiresUnreferencedCodeAnalyzer;

namespace ILLink.RoslynAnalyzer
{
	static class AnalyzerUtils
	{
		/// <summary>
		/// Returns true if <see paramref="type" /> has the same name as <see paramref="typename" />
		/// </summary>
		internal static bool HasName (this INamedTypeSymbol type, string typeName)
		{
			var roSpan = typeName.AsSpan ();
			INamespaceOrTypeSymbol? currentType = type;
			while (roSpan.Length > 0) {
				var dot = roSpan.LastIndexOf ('.');
				var currentName = dot < 0 ? roSpan : roSpan.Slice (dot + 1);
				if (currentType is null ||
					!currentName.Equals (currentType.Name.AsSpan (), StringComparison.Ordinal)) {
					return false;
				}
				currentType = (INamespaceOrTypeSymbol?) currentType.ContainingType ?? currentType.ContainingNamespace;
				roSpan = roSpan.Slice (0, dot > 0 ? dot : 0);
			}

			return true;
		}

		const string SuppressMessageFqn = "System.Diagnostics.CodeAnalysis.UnconditionalSuppressMessageAttribute";

		internal static bool IsDiagnosticSuppressed(OperationAnalysisContext operationContext, string diagnosticId)
		{
			var attributes = operationContext.ContainingSymbol.GetAttributes();
			foreach (var attr in attributes) {
				if (attr.AttributeClass?.HasName (RequiresUnreferencedCodeFqn) == true) {
					return true;
				}

				if (attr.AttributeClass?.HasName (SuppressMessageFqn) == true
				    && TryDecodeSuppressMessageId(attr, out var id)
					&& string.Equals(id, diagnosticId, StringComparison.OrdinalIgnoreCase)) {
					return true;
				}
			}

			return false;
		}

		private static bool TryDecodeSuppressMessageId (AttributeData attribute, out string? id)
		{
			id = null;

            // We need at least the Category and Id to decode the diagnostic to suppress.
            // The only SuppressMessageAttribute constructor requires those two parameters.
            if (attribute.ConstructorArguments.Length != 2)
            {
                return false;
            }

            id = attribute.ConstructorArguments[1].Value as string;
            if (id == null)
            {
                return false;
            }

            var separatorIndex = id.IndexOf(':');
            if (separatorIndex != -1)
            {
                id = id.Remove(separatorIndex);
            }

            return true;
        }
	}
}
