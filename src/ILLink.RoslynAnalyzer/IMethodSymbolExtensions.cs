// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	public static class IMethodSymbolExtension
	{
		/// <summary>
		/// Returns whether or not the ILParameterIndex represents the `this` Parameter
		/// </summary>
		public static bool IsThisParameterIndex (this IMethodSymbol method, ILParameterIndex index)
			=> method.IsStatic ? false : index == 0;

		/// <summary>
		/// Returns the IParameterSymbol that corresponds to the ILParameterIndex <paramref name="index"/>.
		/// Throws if <paramref name="index"/> corresponds to the `this` parameter.
		/// Guard with <see cref="IsThisParameterIndex(IMethodSymbol, ILParameterIndex)"/> to avoid throwing.
		/// </summary>
		/// <exception cref="InvalidOperationException">Throws if the ILParameterIndex corresponds to the `this` parameter.</exception>
		public static IParameterSymbol? GetParameter (this IMethodSymbol method, ILParameterIndex index)
		{
			if (method.IsThisParameterIndex (index))
				return null;
			int paramIndex = (int) method.GetNonThisParameterIndex (index);
			if (paramIndex >= method.Parameters.Length)
				return null;
			return method.Parameters[paramIndex];
		}

		/// <summary>
		/// Returns the number of parameters in IL (including implicit `this`)
		/// </summary>
		public static int GetILParameterCount (this IMethodSymbol method)
		{
			if (method.IsStatic)
				return method.Parameters.Length;
			else
				return method.Parameters.Length + 1;
		}

		public static int GetNonThisParameterCount (this IMethodSymbol method)
			=> method.Parameters.Length;

		/// <summary>
		/// Returns the type of the parameter at the index provided, or null if the index is out of range.
		/// </summary>
		public static ITypeSymbol? GetParameterType (this IMethodSymbol method, ILParameterIndex index)
		{
			if (method.IsThisParameterIndex (index))
				return method.ContainingType;
			return method.GetParameter (index)?.Type;
		}

		public static Location GetParameterLocation (this IMethodSymbol method, ILParameterIndex index)
		{
			if (method.IsThisParameterIndex (index))
				return method.Locations[0];
			return method.GetParameter (index)!.Locations[0];
		}

		public static DynamicallyAccessedMemberTypes GetParameterDynamicallyAccessedMemberTypes (this IMethodSymbol method, ILParameterIndex index)
		{
			if (method.IsThisParameterIndex (index))
				return method.GetDynamicallyAccessedMemberTypes ();
			return method.GetParameter (index)!.GetDynamicallyAccessedMemberTypes ();
		}

		public static NonThisParameterIndex GetNonThisParameterIndex (this IMethodSymbol method, ILParameterIndex index)
			=> (NonThisParameterIndex) (method.IsStatic ? index : index - 1);

		public static ILParameterIndex GetILParameterIndex (this IMethodSymbol method, NonThisParameterIndex index)
			=> (ILParameterIndex) (method.IsStatic ? index : index + 1);
	}
}