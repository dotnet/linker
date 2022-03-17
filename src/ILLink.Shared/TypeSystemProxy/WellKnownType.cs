
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace ILLink.Shared.TypeSystemProxy
{
	public enum WellKnownType
	{
		System_String,
		System_Nullable_T,
		System_Type,
		System_Reflection_IReflect,
		System_Array,
		System_Object,
		System_Attribute
	}

	public static partial class WellKnownTypeExtensions
	{
		public static (string Namespace, string Name) GetNamespaceAndName (this WellKnownType type)
		{
			switch (type) {
			case WellKnownType.System_String:
				return ("System", "String");
			case WellKnownType.System_Nullable_T:
				return ("System", "Nullable`1");
			case WellKnownType.System_Type:
				return ("System", "Type");
			case WellKnownType.System_Reflection_IReflect:
				return ("System.Reflection", "IReflect");
			case WellKnownType.System_Array:
				return ("System", "Array");
			case WellKnownType.System_Object:
				return ("System", "Object");
			case WellKnownType.System_Attribute:
				return ("System", "Attribute");
			default:
				throw new ArgumentException ($"{nameof (type)} is not a well-known type.");
			}
		}
		public static string GetNamespace (this WellKnownType type) => GetNamespaceAndName (type).Namespace;
		public static string GetName (this WellKnownType type) => GetNamespaceAndName (type).Name;
	}
}