// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using ILLink.Shared;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
#pragma warning disable RS0030 // MethodReference.Parameters wrappers are defined here
	internal static class MethodReferenceExtensions
	{
		public static string GetDisplayName (this MethodReference method)
		{
			var sb = new System.Text.StringBuilder ();

			// Match C# syntaxis name if setter or getter
			var methodDefinition = method.Resolve ();
			if (methodDefinition != null && (methodDefinition.IsSetter || methodDefinition.IsGetter)) {
				// Append property name
				string name = methodDefinition.IsSetter ? string.Concat (methodDefinition.Name.AsSpan (4), ".set") : string.Concat (methodDefinition.Name.AsSpan (4), ".get");
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());
				return sb.ToString ();
			}

			if (methodDefinition != null && methodDefinition.IsEventMethod ()) {
				// Append event name
				string name = methodDefinition.SemanticsAttributes switch {
					MethodSemanticsAttributes.AddOn => string.Concat (methodDefinition.Name.AsSpan (4), ".add"),
					MethodSemanticsAttributes.RemoveOn => string.Concat (methodDefinition.Name.AsSpan (7), ".remove"),
					MethodSemanticsAttributes.Fire => string.Concat (methodDefinition.Name.AsSpan (6), ".raise"),
					_ => throw new NotSupportedException (),
				};
				sb.Append (name);
				// Insert declaring type name and namespace
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());
				return sb.ToString ();
			}

			// Append parameters
			sb.Append ("(");
			if (method.HasParameters) {
				for (int i = 0; i < method.Parameters.Count - 1; i++)
					sb.Append (method.Parameters[i].ParameterType.GetDisplayNameWithoutNamespace ()).Append (", ");

				sb.Append (method.Parameters[method.Parameters.Count - 1].ParameterType.GetDisplayNameWithoutNamespace ());
			}

			sb.Append (")");

			// Insert generic parameters
			if (method.HasGenericParameters) {
				TypeReferenceExtensions.PrependGenericParameters (method.GenericParameters, sb);
			}

			// Insert method name
			if (method.Name == ".ctor")
				sb.Insert (0, method.DeclaringType.Name);
			else
				sb.Insert (0, method.Name);

			// Insert declaring type name and namespace
			if (method.DeclaringType != null)
				sb.Insert (0, '.').Insert (0, method.DeclaringType.GetDisplayName ());

			return sb.ToString ();
		}

		public static TypeReference? GetReturnType (this MethodReference method, LinkContext context)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.ReturnType, context);

			return method.ReturnType;
		}

		public static bool ReturnsVoid (this IMethodSignature method)
		{
			return method.ReturnType.WithoutModifiers ().MetadataType == MetadataType.Void;
		}

		public static TypeReference? GetInflatedParameterType (this MethodReference method, int parameterIndex, LinkContext context)
		{
			if (method.DeclaringType is GenericInstanceType genericInstance)
				return TypeReferenceExtensions.InflateGenericType (genericInstance, method.Parameters[parameterIndex].ParameterType, context);

			return method.Parameters[parameterIndex].ParameterType;
		}

		public static TypeReference GetParameterType (this MethodReference method, ParameterIndex parameterIndex)
			=> method.Parameters[(int) parameterIndex].ParameterType;

		public static TypeReference GetParameterType (this MethodReference method, ILParameterIndex parameterIndex)
			=> method.IsImplicitThisParameter (parameterIndex) ?
				method.DeclaringType
				: method.Parameters[(int) method.GetNonThisParameterIndex (parameterIndex)].ParameterType;

		public static ParameterDefinition? TryGetParameter (this MethodReference method, ILParameterIndex index)
			=> method.IsImplicitThisParameter (index) ?
				null
				: method.Parameters[(int) method.GetNonThisParameterIndex (index)];

		public static ParameterIndex GetNonThisParameterIndex (this MethodReference method, ILParameterIndex ilParameterIndex)
		{
			if (method.IsImplicitThisParameter (ilParameterIndex))
				throw new InvalidOperationException ("Cannot get non-this parameter index for `this` parameter.");
			return (ParameterIndex) (method.HasImplicitThis () ? ilParameterIndex - 1 : ilParameterIndex);
		}

		public static ILParameterIndex GetILParameterIndex (this MethodReference method, ParameterIndex index)
			=> (ILParameterIndex) (method.HasImplicitThis () ? index + 1 : index);

		public static bool IsImplicitThisParameter (this MethodReference method, ILParameterIndex parameterIndex)
			=> parameterIndex == 0 && method.HasImplicitThis ();

		/// <summary>
		/// Returns the number of parameters in IL (including the implicit `this`)
		/// </summary>
		public static int GetILArgumentCount (this MethodReference method)
			=> method.HasImplicitThis () ? method.Parameters.Count + 1 : method.Parameters.Count;

		public static int GetNonThisParameterCount (this MethodReference method)
			=> method.Parameters.Count;

		public static bool IsDeclaredOnType (this MethodReference method, string fullTypeName)
		{
			return method.DeclaringType.IsTypeOf (fullTypeName);
		}

		public static bool HasParameterOfType (this MethodReference method, ILParameterIndex parameterIndex, string fullTypeName)
		{
			if ((int) parameterIndex >= method.GetILArgumentCount ())
				return false;
			return method.GetParameterType (parameterIndex).IsTypeOf (fullTypeName);
		}

		public static bool HasImplicitThis (this MethodReference method)
		{
			return method.HasThis && !method.ExplicitThis;
		}

		public static IEnumerable<ReferenceKind> GetParameterReferenceKinds (this MethodReference method)
		{
			if (method.HasImplicitThis ())
				yield return method.DeclaringType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
			foreach (var parameter in method.Parameters)
				yield return GetReferenceKind (parameter);
		}

		public static ReferenceKind GetReferenceKind (ParameterDefinition param)
		{
			if (!param.ParameterType.IsByReference)
				return ReferenceKind.None;
			if (param.IsIn)
				return ReferenceKind.In;
			if (param.IsOut)
				return ReferenceKind.Out;
			return ReferenceKind.Ref;
		}

		public static ReferenceKind GetParameterReferenceKind (this MethodReference method, ParameterIndex index)
		{
			if (index is ParameterIndex.This) {
				return method.DeclaringType.IsValueType ? ReferenceKind.Ref : ReferenceKind.None;
			}
			var param = method.Parameters[index];
			if (!param.ParameterType.IsByReference)
				return ReferenceKind.None;
			if (param.IsIn)
				return ReferenceKind.In;
			if (param.IsOut)
				return ReferenceKind.Out;
			return ReferenceKind.Ref;
		}
		public static IEnumerable<ParameterIndex> GetParameterIndices (this MethodReference method)
		{
			for (ParameterIndex i = method.HasImplicitThis () ? ParameterIndex.This : 0; i < method.Parameters.Count; i++)
				yield return i;
		}
	}
#pragma warning restore RS0030 // MethodReference.Parameters wrappers are defined here
}
