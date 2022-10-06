﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	partial struct ParameterProxy
	{
		public ParameterProxy (MethodProxy method, ParameterIndex index)
		{
			if ((int) index < 0 || (int) index >= method.GetParametersCount ())
				throw new InvalidOperationException ($"Parameter of index {(int) index} does not exist on method {method.GetDisplayName ()} with {method.GetParametersCount ()} parameters");
			Method = method;
			Index = index;
		}

		public ParameterProxy (IParameterSymbol parameter)
		{
			Method = new ((IMethodSymbol) parameter.ContainingSymbol);
			Index = (ParameterIndex) parameter.Ordinal + (Method.HasImplicitThis () ? 1 : 0);
		}

		public ReferenceKind ReferenceKind =>
			IsImplicitThis ? ((ITypeSymbol) Method.Method.ContainingSymbol).IsValueType ? ReferenceKind.Ref : ReferenceKind.None
			: Method.Method.Parameters[MetadataIndex].RefKind switch {
				RefKind.Ref => ReferenceKind.Ref,
				RefKind.In => ReferenceKind.In,
				RefKind.Out => ReferenceKind.Out,
				RefKind.None => ReferenceKind.None,
				_ => throw new NotImplementedException ($"Unexpected RefKind found on parameter {GetDisplayName ()}")
			};

		public MethodProxy Method { get; }

		public ParameterIndex Index { get; }

		/// <summary>
		/// Returns the IParameterSymbol representing the parameter. Returns null for the implicit this paramter.
		/// </summary>
		public IParameterSymbol? ParameterSymbol => IsImplicitThis ? null : Method.Method.Parameters[MetadataIndex];

		/// <summary>
		/// Returns the IParameterSymbol.Location[0] for the parameter. Returns null for the implicit this paramter.
		/// </summary>
		public Location? Location => ParameterSymbol?.Locations[0];

		public int MetadataIndex {
			get {
				if (Method.HasImplicitThis ()) {
					if (IsImplicitThis)
						throw new InvalidOperationException ("Cannot get metadata index of the implicit 'this' parameter");
					return (int) Index - 1;
				}
				return (int) Index;
			}
		}

		public TypeProxy ParameterType
			=> IsImplicitThis ?
				new TypeProxy (Method.Method.ContainingType)
				: new TypeProxy (Method.Method.Parameters[MetadataIndex].Type);

		public partial string GetDisplayName ()
		{
			if (IsImplicitThis)
				return Method.GetDisplayName ();
			return ParameterSymbol!.GetDisplayName ();
		}

		public partial bool IsTypeOf (string typeName) => ParameterType.IsTypeOf (typeName.Substring (0, typeName.LastIndexOf ('.')), typeName.Substring (1 + typeName.LastIndexOf ('.')));

		public bool IsTypeOf (WellKnownType type) => ParameterType.IsTypeOf (type);
	}
}