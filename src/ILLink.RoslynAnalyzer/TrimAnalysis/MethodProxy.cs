// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial struct MethodProxy
	{
		public MethodProxy (IMethodSymbol method) => Method = method;

		public readonly IMethodSymbol Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => IsTypeOf (Method.ContainingType, fullTypeName);

		internal partial bool HasNonThisParameters () => Method.Parameters.Length > 0;

		internal partial int GetNonThisParametersCount () => Method.Parameters.Length;

		internal partial int GetILParametersCount () => Method.Parameters.Length + (Method.IsStatic ? 0 : 1);

		internal partial string GetParameterDisplayName (ILParameterIndex parameterIndex) => Method.GetParameter (parameterIndex)!.GetDisplayName ();

		internal partial List<ParameterProxy> GetParameters () => Method.GetParameters ();

		internal partial ParameterProxy GetParameter (ParameterIndex index) => Method.GetParameter (index);

		internal partial bool HasGenericParameters () => Method.IsGenericMethod;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.TypeParameters.Length == genericParameterCount;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (Method.TypeParameters.IsEmpty)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Method.TypeParameters.Length);
			foreach (var typeParameter in Method.TypeParameters) {
				builder.Add (new GenericParameterProxy (typeParameter));
			}

			return builder.ToImmutableArray ();
		}

		internal partial bool IsStatic () => Method.IsStatic;
		internal partial bool HasImplicitThis () => Method.IsStatic;

		internal partial bool ReturnsVoid () => Method.ReturnType.SpecialType == SpecialType.System_Void;

		static bool IsTypeOf (ITypeSymbol type, string fullTypeName)
		{
			if (type is not INamedTypeSymbol namedType)
				return false;

			return namedType.HasName (fullTypeName);
		}

		public ReferenceKind GetParameterReferenceKind (ILParameterIndex index)
		{
			if (Method.IsThisParameterIndex (index))
				return Method.ContainingType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
			return Method.GetParameter (index)!.RefKind switch {
				RefKind.In => ReferenceKind.In,
				RefKind.Out => ReferenceKind.Out,
				RefKind.Ref => ReferenceKind.Ref,
				_ => ReferenceKind.None
			};
		}

		internal partial ILParameterIndex GetILParameterIndex (ParameterIndex parameterIndex)
			=> Method.GetILParameterIndex (parameterIndex);

		internal partial ParameterIndex GetNonThisParameterIndex (ILParameterIndex parameterIndex)
			=> Method.GetNonThisParameterIndex (parameterIndex);

		public override string ToString () => Method.ToString ();
	}
}
