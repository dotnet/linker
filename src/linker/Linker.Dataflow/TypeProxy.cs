// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	internal readonly partial struct TypeProxy
	{
		public TypeProxy (TypeDefinition type) : this (type, null)
		{ }

		private TypeProxy (TypeDefinition type, TypeDefinition? underlyingType)
		{
			Type = type;
			NullableUnderlyingType = underlyingType;
		}

		public static TypeProxy NullableTypeProxy (TypeDefinition nullableType, TypeDefinition underlyingType)
		{
			Debug.Assert (nullableType.Name == "Nullable`1" && nullableType.Namespace == "System");
			return new TypeProxy (nullableType, underlyingType);
		}

		public static implicit operator TypeProxy (TypeDefinition type) => new (type);

		public TypeDefinition Type { get; }

		public TypeDefinition? NullableUnderlyingType { get; }

		public string Name { get => Type.Name; }

		public string GetDisplayName () => Type.GetDisplayName ();

		public override string ToString () => Type.ToString ();
	}
}
