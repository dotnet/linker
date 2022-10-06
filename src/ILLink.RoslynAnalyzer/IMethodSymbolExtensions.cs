// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.RoslynAnalyzer
{
	internal static class IMethodSymbolExtensions
	{
		public static bool HasImplicitThis (this IMethodSymbol method)
		{
			return !method.IsStatic;
		}

		/// <summary>
		/// Returns a list of the parameters pushed onto the stack before the method call (including the implicit 'this' parameter)
		/// </summary>
		public static List<ParameterProxy> GetParameters (this IMethodSymbol method)
		{
			List<ParameterProxy> parameters = new ();
			int paramsNum = method.GetParametersCount ();
			for (int i = 0; i < paramsNum; i++)
				parameters.Add (new (new MethodProxy (method), (ParameterIndex) i));
			return parameters;
		}

		/// <summary>
		/// Returns a list of the parameters in the method's 'parameters' metadata section (i.e. excluding the implicit 'this' parameter)
		/// </summary>
		public static List<ParameterProxy> GetMetadataParameters (this IMethodSymbol method)
		{
			List<ParameterProxy> parameters = new ();
			int i = 0;
			if (method.HasImplicitThis ()) {
				i++;
			}
			int paramsCount = method.Parameters.Length + i;
			for (; i < paramsCount; i++)
				parameters.Add (new (new MethodProxy (method), (ParameterIndex) i));
			return parameters;
		}

		/// <summary>
		/// Gets the parameter at the <see cref="ParameterIndex"/> provided. Note ParameterIndex treat the implicit 'this' as index 0.
		/// Throws if the index is out of bounds.
		/// </summary>
		public static ParameterProxy GetParameter (this IMethodSymbol method, ParameterIndex index)
		{
			if (method.TryGetParameter (index) is not ParameterProxy param)
				throw new InvalidOperationException ($"Cannot get parameter at index {(int) index} of method {method.GetDisplayName ()} with {(int) method.GetParametersCount ()} parameters.");
			return param;
		}

		/// <summary>
		/// Gets the parameter at the <see cref="ParameterIndex"/> provided. Note ParameterIndex treat the implicit 'this' as index 0.
		/// Returns null if the index is out of bounds.
		/// </summary>
		public static ParameterProxy? TryGetParameter (this IMethodSymbol method, ParameterIndex index)
		{
			if (method.GetParametersCount () <= (int) index || (int) index < 0)
				return null;
			return new ParameterProxy (new (method), index);
		}

		/// <summary>
		/// Gets the number of entries in the 'Parameters' section of a method's metadata (i.e. excludes the implicit 'this' from the count)
		/// </summary>
		public static int GetMetadataParametersCount (this IMethodSymbol method)
		{
			return method.Parameters.Length;
		}

		/// <summary>
		/// Gets the number of parameters pushed onto the stack when the method is called (including the implicit 'this' paramter)
		/// </summary>
		public static int GetParametersCount (this IMethodSymbol method)
		{
			//return method.Arity + (method.HasImplicitThis () ? 1 : 0);
			return method.Parameters.Length + (method.HasImplicitThis () ? 1 : 0);
		}
	}
}