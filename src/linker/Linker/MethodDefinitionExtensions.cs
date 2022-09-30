// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using ILLink.Shared.TypeSystemProxy;
using Mono.Cecil;

namespace Mono.Linker
{
	internal static class MethodDefinitionExtensions
	{
		public static bool IsDefaultConstructor (this MethodDefinition method)
		{
			return IsInstanceConstructor (method) && !method.HasParameters;
		}

		public static bool IsInstanceConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && !method.IsStatic;
		}

		public static bool IsIntrinsic (this MethodDefinition method)
		{
			if (!method.HasCustomAttributes)
				return false;

			foreach (var ca in method.CustomAttributes) {
				var caType = ca.AttributeType;
				if (caType.Name == "IntrinsicAttribute" && caType.Namespace == "System.Runtime.CompilerServices")
					return true;
			}

			return false;
		}

		public static bool IsPropertyMethod (this MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.Getter) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Setter) != 0;
		}

		public static bool IsPublicInstancePropertyMethod (this MethodDefinition md)
		{
			return md.IsPublic && !md.IsStatic && IsPropertyMethod (md);
		}

		public static bool IsEventMethod (this MethodDefinition md)
		{
			return (md.SemanticsAttributes & MethodSemanticsAttributes.AddOn) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.Fire) != 0 ||
				(md.SemanticsAttributes & MethodSemanticsAttributes.RemoveOn) != 0;
		}

		public static bool TryGetProperty (this MethodDefinition md, [NotNullWhen (true)] out PropertyDefinition? property)
		{
			property = null;
			if (!md.IsPropertyMethod ())
				return false;

			TypeDefinition declaringType = md.DeclaringType;
			foreach (PropertyDefinition prop in declaringType.Properties)
				if (prop.GetMethod == md || prop.SetMethod == md) {
					property = prop;
					return true;
				}

			return false;
		}

		public static bool TryGetEvent (this MethodDefinition md, [NotNullWhen (true)] out EventDefinition? @event)
		{
			@event = null;
			if (!md.IsEventMethod ())
				return false;

			TypeDefinition declaringType = md.DeclaringType;
			foreach (EventDefinition evt in declaringType.Events)
				if (evt.AddMethod == md || evt.InvokeMethod == md || evt.RemoveMethod == md) {
					@event = evt;
					return true;
				}

			return false;
		}

		public static bool IsStaticConstructor (this MethodDefinition method)
		{
			return method.IsConstructor && method.IsStatic;
		}

		public static void ClearDebugInformation (this MethodDefinition method)
		{
			// TODO: This always allocates, update when Cecil catches up
			var di = method.DebugInformation;
			di.SequencePoints.Clear ();
			if (di.Scope != null) {
				di.Scope.Variables.Clear ();
				di.Scope.Constants.Clear ();
				di.Scope = null;
			}
		}

		public static ParameterProxy GetParameter (this MethodDefinition method, ParameterIndex index)
		{
			if (method.HasImplicitThis ())
				return new (new (method), ParameterIndex.This);
			return new (new (method), index);
		}

		[SuppressMessage ("ApiDesign", "RS0030:Do not used banned APIs", Justification = "This provides the wrapper around the Parameters property")]
		public static List<ParameterProxy> GetParameters (this MethodDefinition method)
		{
			List<ParameterProxy> parameters = new ();
			if (method.HasImplicitThis ())
				parameters.Add (new (new (method), ParameterIndex.This));
			int paramsCount = method.Parameters.Count;
			for (ParameterIndex i = 0; i < paramsCount; i++) {
				parameters.Add (new (new (method), i));
			}
			return parameters;
		}

		public static ICustomAttributeProvider GetParameterCustomAttributeProvider (this MethodDefinition method, ILParameterIndex index)
		{
			if (method.IsImplicitThisParameter (index))
				return method;
			return method.GetParameter (index);
		}
	}
}
