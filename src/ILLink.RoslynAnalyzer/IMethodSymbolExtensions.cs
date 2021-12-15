// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	static class IMethodSymbolExtensions
	{
		public static bool IsSetter (this IMethodSymbol method)
		{
			return method.MethodKind == MethodKind.PropertySet;
		}

		public static bool IsGetter (this IMethodSymbol method)
		{
			return method.MethodKind == MethodKind.PropertyGet;
		}
		public static bool IsEventMethod (this IMethodSymbol method)
		{
			return method.MethodKind == MethodKind.EventAdd
				|| method.MethodKind == MethodKind.EventRaise
				|| method.MethodKind == MethodKind.EventRemove;
		}
		private static void PrependGenericParameters(ImmutableArray<ITypeParameterSymbol> genericParameters, System.Text.StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericParameters[genericParameters.Length - 1]);
			for (int i = genericParameters.Length - 2; i >= 0; i--)
				sb.Insert (0, ',').Insert (0, genericParameters[i]);

			sb.Insert (0, '<');
		}

		public static string GetDisplayName (this IMethodSymbol method)
		{
			var sb = new System.Text.StringBuilder ();

			// Match C# syntaxis name if setter or getter
			if (method != null && (method.IsSetter() || method.IsGetter())) {
				// Append property name
				string name = method.IsSetter() ? string.Concat (method.Name, ".set") : string.Concat (method.Name, ".get");
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.ContainingType.GetDisplayName ());
				return sb.ToString ();
			}

			if (method != null && method.IsEventMethod ()) {
				// Append event name
				string name = method.MethodKind switch {
					MethodKind.EventAdd => string.Concat (method.Name, ".add"),
					MethodKind.EventRemove => string.Concat (method.Name, ".remove"),
					MethodKind.EventRaise => string.Concat (method.Name, ".raise"),
					_ => throw new NotSupportedException (),
				};
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.ContainingType.GetDisplayName ());
				return sb.ToString ();
			}

			if (method.IsConstructor ())
				sb.Append (".ctor");
			else if (method.IsStaticConstructor ())
				sb.Append (".cctor");
			
			// Append parameters
			sb.Append ("(");
			if (method?.Parameters.Length > 0) {
				for (int i = 0; i < method.Parameters.Length - 1; i++)
					sb.Append (method.Parameters[i].GetDisplayName()).Append (',');

				sb.Append (method.Parameters[method.Parameters.Length - 1].GetDisplayName ());
			}

			sb.Append (")");

			// Insert generic parameters
			if (method is not null && method.IsGenericMethod) {
				PrependGenericParameters (method.TypeParameters, sb);
			}			

			// Insert declaring type name and namespace
			if (method is not null && method.ContainingType != null)
				sb.Insert (0, '.').Insert (0, method.ContainingType.GetDisplayName ());

			return sb.ToString ();
		}
	}
}
