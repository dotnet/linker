// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
	[SuppressMessage ("ApiDesign", "RS0030:Do not used banned APIs", Justification = "This class provides wrapper methods around the banned Parameters property")]
	internal partial struct ParameterProxy
	{
		public ReferenceKind ReferenceKind {
			get {
				if (IsImplicitThis)
					return Method.Method.DeclaringType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
				var param = Method.Method.Parameters[MetadataIndex];
				if (!param.ParameterType.IsByReference)
					return ReferenceKind.None;
				if (param.IsIn)
					return ReferenceKind.In;
				if (param.IsOut)
					return ReferenceKind.Out;
				return ReferenceKind.Ref;
			}
		}

		public TypeReference ParameterType {
			get {
				if (IsImplicitThis)
					return Method.Method.DeclaringType;
				return Method.Method.Parameters[MetadataIndex].ParameterType;
			}
		}

		public partial string GetDisplayName () => IsImplicitThis ? Method.GetDisplayName ()
			: !string.IsNullOrEmpty (Method.Method.Parameters[MetadataIndex].Name) ? Method.Method.Parameters[MetadataIndex].Name
			: $"#{Index}";

		public ICustomAttributeProvider GetCustomAttributeProvider ()
		{
			if (IsImplicitThis)
				return Method.Method;
			return Method.Method.Parameters[MetadataIndex];
		}

		public partial bool IsTypeOf (string typeName) => ParameterType.IsTypeOf (typeName);

		public bool IsTypeOf (WellKnownType type) => ParameterType.IsTypeOf (type);
	}
}
