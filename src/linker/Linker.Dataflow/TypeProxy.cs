// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;
using Mono.Cecil;
using Mono.Linker;
using static System.Runtime.CompilerServices.MethodImplOptions;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeDefinition type) => Type = type;

		public static implicit operator TypeProxy (TypeDefinition type) => new (type);

		public TypeDefinition Type { get; }

		public string Name { get => Type.Name; }

		public string? Namespace { get => Type.Namespace; }

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
