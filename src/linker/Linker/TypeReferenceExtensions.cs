﻿using System;
using Mono.Cecil;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mono.Linker
{
	public static class TypeReferenceExtensions
	{
		public static string GetDisplayName (this TypeReference type)
		{
			if (type == null)
				return string.Empty;

			StringBuilder sb = new StringBuilder ();
			var typeNamespace = type.GetNamespaceDisplayName ();
			if (typeNamespace != string.Empty)
				sb.Append (typeNamespace).Append ('.');

			sb.Append (type.GetTypeReferenceDisplayName ());
			return sb.ToString ();
		}

		static string GetNamespaceDisplayName (this TypeReference type)
		{
			var typeNamespace = string.Empty;
			while (type != null) {
				typeNamespace = type.Namespace;
				type = type.DeclaringType;
			}

			return typeNamespace;
		}

		internal static string GetTypeReferenceDisplayName (this TypeReference type)
		{
			if (type == null)
				return string.Empty;

			var sb = new StringBuilder ();
			Stack<TypeReference> genericArguments = null;
			while (true) {
				switch (type) {
				case ArrayType arrayType:
					ParseArrayType (arrayType, sb);
					break;
				case GenericInstanceType genericInstanceType:
					int arguments = int.Parse (genericInstanceType.Name.Substring (genericInstanceType.Name.IndexOf ('`') + 1));
					genericArguments = new Stack<TypeReference> (genericInstanceType.GenericArguments);
					ParseGenericArguments (genericArguments, arguments, sb);
					sb.Insert (0, genericInstanceType.Name.Substring (0, genericInstanceType.Name.IndexOf ('`')));
					break;
				default:
					if (type.HasGenericParameters) {
						if (!int.TryParse (type.Name.Substring (type.Name.IndexOf ('`') + 1), out int arity)) {
							sb.Insert (0, type.Name);
							break;
						}

						if (genericArguments?.Count > 0)
							ParseGenericArguments (genericArguments, arity, sb);
						else
							ParseGenericParameters (type.GenericParameters.Skip (type.GenericParameters.Count - arity).ToList (), sb);

						sb.Insert (0, type.Name.Substring (0, type.Name.IndexOf ('`')));
						break;
					}

					sb.Insert (0, type.Name);
					break;
				}

				type = type.DeclaringType;
				if (type == null)
					break;

				sb.Insert (0, '.');
			}

			return sb.ToString ();
		}

		internal static void ParseGenericParameters (IList<GenericParameter> genericParameters, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericParameters[genericParameters.Count - 1]);
			for (int i = genericParameters.Count - 2; i >= 0; i--)
				sb.Insert (0, ',').Insert (0, genericParameters[i]);

			sb.Insert (0, '<');
		}

		private static void ParseGenericArguments (Stack<TypeReference> genericArguments, int argumentsToTake, StringBuilder sb)
		{
			sb.Insert (0, '>').Insert (0, genericArguments.Pop ().GetTypeReferenceDisplayName ());
			while (--argumentsToTake > 0)
				sb.Insert (0, ',').Insert (0, genericArguments.Pop ().GetTypeReferenceDisplayName ());

			sb.Insert (0, '<');
		}

		private static void ParseArrayType (ArrayType arrayType, StringBuilder sb)
		{
			void parseArrayDimensions (ArrayType at)
			{
				sb.Append ('[');
				for (int i = 0; i < at.Dimensions.Count - 1; i++)
					sb.Append (',');

				sb.Append (']');
			}

			sb.Append (arrayType.Name.Substring (0, arrayType.Name.IndexOf ('[')));
			parseArrayDimensions (arrayType);
			var element = arrayType.ElementType as ArrayType;
			while (element != null) {
				parseArrayDimensions (element);
				element = element.ElementType as ArrayType;
			}
		}

		public static TypeReference GetInflatedBaseType (this TypeReference type)
		{
			if (type == null)
				return null;

			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			if (type is SentinelType sentinelType)
				return sentinelType.ElementType.GetInflatedBaseType ();

			if (type is PinnedType pinnedType)
				return pinnedType.ElementType.GetInflatedBaseType ();

			if (type is RequiredModifierType requiredModifierType)
				return requiredModifierType.ElementType.GetInflatedBaseType ();

			if (type is GenericInstanceType genericInstance) {
				var baseType = type.Resolve ()?.BaseType;

				if (baseType is GenericInstanceType)
					return InflateGenericType (genericInstance, baseType);

				return baseType;
			}

			return type.Resolve ()?.BaseType;
		}

		public static TypeReference GetInflatedDeclaringType (this TypeReference type)
		{
			if (type == null)
				return null;

			if (type.IsGenericParameter || type.IsByReference || type.IsPointer)
				return null;

			if (type is SentinelType sentinelType)
				return sentinelType.ElementType.GetInflatedDeclaringType ();

			if (type is PinnedType pinnedType)
				return pinnedType.ElementType.GetInflatedDeclaringType ();

			if (type is RequiredModifierType requiredModifierType)
				return requiredModifierType.ElementType.GetInflatedDeclaringType ();

			if (type is GenericInstanceType genericInstance) {
				var declaringType = genericInstance.DeclaringType;

				if (declaringType.HasGenericParameters) {
					var result = new GenericInstanceType (declaringType);
					for (var i = 0; i < declaringType.GenericParameters.Count; ++i)
						result.GenericArguments.Add (genericInstance.GenericArguments[i]);

					return result;
				}

				return declaringType;
			}

			var resolved = type.Resolve ();
			System.Diagnostics.Debug.Assert (resolved == type);
			return resolved?.DeclaringType;
		}

		public static IEnumerable<TypeReference> GetInflatedInterfaces (this TypeReference typeRef)
		{
			var typeDef = typeRef.Resolve ();

			if (typeDef?.HasInterfaces != true)
				yield break;

			if (typeRef is GenericInstanceType genericInstance) {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return InflateGenericType (genericInstance, interfaceImpl.InterfaceType);
			} else {
				foreach (var interfaceImpl in typeDef.Interfaces)
					yield return interfaceImpl.InterfaceType;
			}
		}

		public static TypeReference InflateGenericType (GenericInstanceType genericInstanceProvider, TypeReference typeToInflate)
		{
			if (typeToInflate is ArrayType arrayType) {
				var inflatedElementType = InflateGenericType (genericInstanceProvider, arrayType.ElementType);

				if (inflatedElementType != arrayType.ElementType)
					return new ArrayType (inflatedElementType, arrayType.Rank);

				return arrayType;
			}

			if (typeToInflate is GenericInstanceType genericInst)
				return MakeGenericType (genericInstanceProvider, genericInst);

			if (typeToInflate is GenericParameter genericParameter) {
				if (genericParameter.Owner is MethodReference)
					return genericParameter;

				var elementType = genericInstanceProvider.ElementType.Resolve ();
				var parameter = elementType.GenericParameters.Single (p => p == genericParameter);
				return genericInstanceProvider.GenericArguments[parameter.Position];
			}

			if (typeToInflate is FunctionPointerType functionPointerType) {
				var result = new FunctionPointerType {
					ReturnType = InflateGenericType (genericInstanceProvider, functionPointerType.ReturnType)
				};

				for (int i = 0; i < functionPointerType.Parameters.Count; i++) {
					var inflatedParameterType = InflateGenericType (genericInstanceProvider, functionPointerType.Parameters[i].ParameterType);
					result.Parameters.Add (new ParameterDefinition (inflatedParameterType));
				}

				return result;
			}

			if (typeToInflate is IModifierType modifierType) {
				var modifier = InflateGenericType (genericInstanceProvider, modifierType.ModifierType);
				var elementType = InflateGenericType (genericInstanceProvider, modifierType.ElementType);

				if (modifierType is OptionalModifierType) {
					return new OptionalModifierType (modifier, elementType);
				}

				return new RequiredModifierType (modifier, elementType);
			}

			if (typeToInflate is PinnedType pinnedType) {
				var elementType = InflateGenericType (genericInstanceProvider, pinnedType.ElementType);

				if (elementType != pinnedType.ElementType)
					return new PinnedType (elementType);

				return pinnedType;
			}

			if (typeToInflate is PointerType pointerType) {
				var elementType = InflateGenericType (genericInstanceProvider, pointerType.ElementType);

				if (elementType != pointerType.ElementType)
					return new PointerType (elementType);

				return pointerType;
			}

			if (typeToInflate is ByReferenceType byReferenceType) {
				var elementType = InflateGenericType (genericInstanceProvider, byReferenceType.ElementType);

				if (elementType != byReferenceType.ElementType)
					return new ByReferenceType (elementType);

				return byReferenceType;
			}

			if (typeToInflate is SentinelType sentinelType) {
				var elementType = InflateGenericType (genericInstanceProvider, sentinelType.ElementType);

				if (elementType != sentinelType.ElementType)
					return new SentinelType (elementType);

				return sentinelType;
			}

			return typeToInflate;
		}

		private static GenericInstanceType MakeGenericType (GenericInstanceType genericInstanceProvider, GenericInstanceType type)
		{
			var result = new GenericInstanceType (type.ElementType);

			for (var i = 0; i < type.GenericArguments.Count; ++i) {
				result.GenericArguments.Add (InflateGenericType (genericInstanceProvider, type.GenericArguments[i]));
			}

			return result;
		}

		public static IEnumerable<MethodReference> GetMethods (this TypeReference type)
		{
			var typeDef = type.Resolve ();

			if (typeDef?.HasMethods != true)
				yield break;

			if (type is GenericInstanceType genericInstanceType) {
				foreach (var methodDef in typeDef.Methods)
					yield return MakeMethodReferenceForGenericInstanceType (genericInstanceType, methodDef);
			} else {
				foreach (var method in typeDef.Methods)
					yield return method;
			}
		}

		private static MethodReference MakeMethodReferenceForGenericInstanceType (GenericInstanceType genericInstanceType, MethodDefinition methodDef)
		{
			var method = new MethodReference (methodDef.Name, methodDef.ReturnType, genericInstanceType) {
				HasThis = methodDef.HasThis,
				ExplicitThis = methodDef.ExplicitThis,
				CallingConvention = methodDef.CallingConvention
			};

			foreach (var parameter in methodDef.Parameters)
				method.Parameters.Add (new ParameterDefinition (parameter.Name, parameter.Attributes, parameter.ParameterType));

			foreach (var gp in methodDef.GenericParameters)
				method.GenericParameters.Add (new GenericParameter (gp.Name, method));

			return method;
		}

		public static string ToCecilName (this string fullTypeName)
		{
			return fullTypeName.Replace ('+', '/');
		}

		public static bool HasDefaultConstructor (this TypeReference type)
		{
			foreach (var m in type.GetMethods ()) {
				if (m.HasParameters)
					continue;

				var definition = m.Resolve ();
				if (definition?.IsDefaultConstructor () == true)
					return true;
			}

			return false;
		}

		public static MethodReference GetDefaultInstanceConstructor (this TypeReference type)
		{
			foreach (var m in type.GetMethods ()) {
				if (m.HasParameters)
					continue;

				var definition = m.Resolve ();
				if (!definition.IsDefaultConstructor ())
					continue;

				return m;
			}

			throw new NotImplementedException ();
		}

		public static bool IsTypeOf (this TypeReference type, string ns, string name)
		{
			return type.Name == name
				&& type.Namespace == ns;
		}

		public static bool IsTypeOf<T> (this TypeReference tr)
		{
			var type = typeof (T);
			return tr.Name == type.Name && tr.Namespace == tr.Namespace;
		}
	}
}
