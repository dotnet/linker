// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Mono.Cecil;
using Mono.Linker;

namespace ILLink.Shared.TypeSystemProxy
{
#pragma warning disable RS0030 // MethodReference.Parameters is banned. This provides the wrappers for Parameters
	internal partial struct ParameterProxy
	{
		public ParameterProxy (MethodProxy method, ParameterIndex index)
		{
			if ((int) index < 0 || (int) index >= method.GetParametersCount ())
				throw new InvalidOperationException ($"Parameter of index {(int) index} does not exist on method {method.GetDisplayName ()} with {method.GetParametersCount ()}");
			Method = method;
			Index = index;
		}

		public MethodProxy Method { get; set; }

		public ParameterIndex Index { get; set; }

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
#pragma warning restore RS0030 // Do not used banned APIs
}
