// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using ILLink.RoslynAnalyzer;
using Microsoft.CodeAnalysis;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (ITypeSymbol type) => Type = type;

		public readonly ITypeSymbol Type;

		public string Name { get => Type.MetadataName; }

		public string? Namespace { get => Type.ContainingNamespace?.Name; }

		[MethodImpl (AggressiveInlining)]
		public bool IsTypeOf (string @namespace, string name) => Type.IsTypeOf (@namespace, name);

		[MethodImpl (AggressiveInlining)]
		public bool IsTypeOf (WellKnownType wellKnownType) => Type.IsTypeOf (wellKnownType);

		[MethodImpl (AggressiveInlining)]
		public string GetDisplayName () => Type.GetDisplayName ();

		[MethodImpl (AggressiveInlining)]
		public override string ToString () => Type.ToString ();
	}
}
