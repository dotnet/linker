// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Immutable;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	readonly partial struct MethodProxy : IEquatable<MethodProxy>
	{
		public MethodProxy (MethodDefinition method) => Method = method;

		public static implicit operator MethodProxy (MethodDefinition method) => new (method);

		public readonly MethodDefinition Method;

		public string Name { get => Method.Name; }

		public string GetDisplayName () => Method.GetDisplayName ();

		internal partial bool IsDeclaredOnType (string fullTypeName) => Method.IsDeclaredOnType (fullTypeName);

		internal partial bool HasNonThisParameters () => Method.HasParameters;

		internal partial int GetNonThisParametersCount () => Method.GetNonThisParameterCount ();

		internal partial int GetILParametersCount () => Method.GetILParameterCount();

		internal partial bool HasParameterOfType (ILParameterIndex parameterIndex, string fullTypeName) => Method.HasParameterOfType (parameterIndex, fullTypeName);

		internal partial string GetParameterDisplayName (ILParameterIndex parameterIndex) => Method.GetParameter(parameterIndex).Name;

		internal partial ILParameterIndex GetILParameterIndex (NonThisParameterIndex parameterIndex)
			=> Method.GetILParameterIndex (parameterIndex);

		internal partial NonThisParameterIndex GetNonThisParameterIndex (ILParameterIndex parameterIndex)
			=> Method.GetNonThisParameterIndex (parameterIndex);

		internal partial bool HasGenericParameters () => Method.HasGenericParameters;

		internal partial bool HasGenericParametersCount (int genericParameterCount) => Method.GenericParameters.Count == genericParameterCount;

		internal partial ImmutableArray<GenericParameterProxy> GetGenericParameters ()
		{
			if (!Method.HasGenericParameters)
				return ImmutableArray<GenericParameterProxy>.Empty;

			var builder = ImmutableArray.CreateBuilder<GenericParameterProxy> (Method.GenericParameters.Count);
			foreach (var genericParameter in Method.GenericParameters) {
				builder.Add (new GenericParameterProxy (genericParameter));
			}

			return builder.ToImmutableArray ();
		}

		internal partial bool IsStatic () => Method.IsStatic;

		internal partial bool ReturnsVoid () => Method.ReturnsVoid ();

		public override string ToString () => Method.ToString ();

		public ReferenceKind ParameterReferenceKind (ILParameterIndex index) => Method.ParameterReferenceKind (index);

		public bool Equals (MethodProxy other) => Method.Equals (other.Method);

		public override bool Equals (object? obj) => obj is MethodProxy other && Equals (other);

		public override int GetHashCode () => Method.GetHashCode ();
	}
}
