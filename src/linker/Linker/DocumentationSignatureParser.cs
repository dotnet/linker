// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker
{
	/// <summary>
	///  Parses a signature for a member, in the format used for C# Documentation Comments:
	///  https://github.com/dotnet/csharplang/blob/master/spec/documentation-comments.md#id-string-format
	///  Adapted from Roslyn's DocumentationCommentId:
	///  https://github.com/dotnet/roslyn/blob/master/src/Compilers/Core/Portable/DocumentationCommentId.cs
	/// </summary>
	///
	/// Roslyn's API works with ISymbol, which represents a symbol exposed by the compiler.
	/// a Symbol has information about the source language, name, metadata name,
	/// containing scopes, visibility/accessibility, attributes, etc.
	/// This API instead works with the Cecil OM. It can be used to refer to IL definitions
	/// where the signature of a member can contain references to instantiated generics.
	///
	public static class DocumentationSignatureParser
	{
		public static IEnumerable<IMemberDefinition> GetSymbolsForDeclarationId (string id, ModuleDefinition module)
		{
			if (id == null)
				throw new ArgumentNullException (nameof (id));

			if (module == null)
				throw new ArgumentNullException (nameof (module));

			var results = new List<IMemberDefinition> ();
			Parser.ParseDeclaredSymbolId (id, module, results);
			return results;
		}

		public static string GetSignaturePart (this TypeReference type)
		{
			var builder = new StringBuilder ();
			DocumentationSignatureGenerator.PartVisitor.Instance.VisitTypeReference (type, builder);
			return builder.ToString ();
		}

		private static class Parser
		{

			enum MemberType
			{
				Type,
				Method,
				Field,
				Property,
				Event,
			}

			public static bool ParseDeclaredSymbolId (string id, ModuleDefinition module, List<IMemberDefinition> results)
			{
				if (id == null)
					return false;

				if (id.Length < 2)
					return false;

				int index = 0;
				results.Clear ();
				ParseDeclaredId (id, ref index, module, results);
				return results.Count > 0;
			}

			private static void ParseDeclaredId (string id, ref int index, ModuleDefinition module, List<IMemberDefinition> results)
			{
				Debug.Assert (results.Count == 0);
				var memberTypeChar = PeekNextChar (id, index);
				MemberType memberType;

				switch (memberTypeChar) {
				case 'E':
					memberType = MemberType.Event;
					break;
				case 'F':
					memberType = MemberType.Field;
					break;
				case 'M':
					memberType = MemberType.Method;
					break;
				case 'N':
					// We do not support namespaces, which do not exist in IL.
					return;
				case 'P':
					memberType = MemberType.Property;
					break;
				case 'T':
					memberType = MemberType.Type;
					break;
				default:
					// Documentation comment id must start with E, F, M, P, or T
					return;
				}

				index++;
				// Note: this allows leaving out the ':'.
				if (PeekNextChar (id, index) == ':')
					index++;

				// Roslyn resolves types by searching namespaces top-down.
				// We don't have namespace info. Instead try treating each part of a
				// dotted name as a type first, then as a namespace if it fails
				// to resolve to a type.
				TypeDefinition? containingType = null;
				var nameBuilder = new StringBuilder ();

				string name;
				int arity;

				// process dotted names
				while (true) {
					name = ParseName (id, ref index);
					// if we are at the end of the dotted name and still haven't resolved it to
					// a type, there are no results.
					if (String.IsNullOrEmpty (name))
						return;
					nameBuilder.Append (name);
					arity = 0;

					// has type parameters?
					if (PeekNextChar (id, index) == '`') {
						index++;

						bool genericType = true;

						// method type parameters?
						// note: this allows `` for type parameters
						if (PeekNextChar (id, index) == '`') {
							index++;
							genericType = false;
						}

						arity = ReadNextInteger (id, ref index);

						if (genericType) {
							// We need to mangle generic type names but not generic method names.
							nameBuilder.Append ('`');
							nameBuilder.Append (arity);
						}
					}

					// no more dots, so don't loop any more
					if (PeekNextChar (id, index) != '.')
						break;

					// must be a namespace or type since name continues after dot
					index++;

					// try to resolve it as a type
					var typeOrNamespaceName = nameBuilder.ToString ();
					GetMatchingTypes (module, declaringType: containingType, name: typeOrNamespaceName, results: results);
					Debug.Assert (results.Count <= 1);
					if (results.Any ()) {
						// the name resolved to a type
						var result = results.Single ();
						Debug.Assert (result is TypeDefinition);
						// result becomes the new container
						containingType = result as TypeDefinition;
						nameBuilder.Clear ();
						results.Clear ();
						continue;
					}

					// it didn't resolve as a type.

					// only types have arity.
					if (arity > 0)
						return;

					// treat it as a namespace and continue building up the type name
					nameBuilder.Append ('.');
				}

				if (containingType == null && memberType != MemberType.Type)
					return;

				var memberName = nameBuilder.ToString ();

				switch (memberType) {
				case MemberType.Method:
					GetMatchingMethods (id, ref index, containingType, memberName, arity, results);
					break;
				case MemberType.Type:
					GetMatchingTypes (module, containingType, memberName, results);
					break;
				case MemberType.Property:
					GetMatchingProperties (id, ref index, containingType, memberName, results);
					break;
				case MemberType.Event:
					GetMatchingEvents (containingType, memberName, results);
					break;
				case MemberType.Field:
					GetMatchingFields (containingType, memberName, results);
					break;
				}
			}
		}

		// Roslyn resolves types in a signature to their declaration by searching through namespaces.
		// To avoid looking for types by name in all referenced assemblies, we just represent types
		// that are part of a signature by their doc comment strings, and we check for matching
		// strings when looking for matching member signatures.
		private static string? ParseTypeSymbol (string id, ref int index, IGenericParameterProvider? typeParameterContext)
		{
			var results = new List<string> ();
			ParseTypeSymbol (id, ref index, typeParameterContext, results);
			if (results.Count == 1)
				return results[0];

			Debug.Assert (results.Count == 0);
			return null;
		}

		private static void ParseTypeSymbol (string id, ref int index, IGenericParameterProvider? typeParameterContext, List<string> results)
		{
			// Note: Roslyn has a special case that deviates from the language spec, which
			// allows context expressions embedded in a type reference => <context-definition>:<type-parameter>
			// We do not support this special format.

			Debug.Assert (results.Count == 0);

			if (PeekNextChar (id, index) == '`')
				ParseTypeParameterSymbol (id, ref index, typeParameterContext, results);
			else
				ParseNamedTypeSymbol (id, ref index, typeParameterContext, results);

			// apply any array or pointer constructions to results
			var startIndex = index;
			var endIndex = index;

			for (int i = 0; i < results.Count; i++) {
				index = startIndex;
				var typeReference = results[i];

				while (true) {
					if (PeekNextChar (id, index) == '[') {
						var boundsStartIndex = index;
						var bounds = ParseArrayBounds (id, ref index);
						var boundsEndIndex = index;
						Debug.Assert (bounds > 0);
						// Instead of constructing a representation of the array bounds, we
						// use the original input to represent the bounds, and later match it
						// against the generated strings for types in signatures.
						// This ensures that we will only resolve members with supported array bounds.
						typeReference += id.Substring (boundsStartIndex, boundsEndIndex - boundsStartIndex);
						continue;
					}

					if (PeekNextChar (id, index) == '*') {
						index++;
						typeReference += '*';
						continue;
					}

					break;
				}

				if (PeekNextChar (id, index) == '@') {
					index++;
					typeReference += '@';
				}

				results[i] = typeReference;
				endIndex = index;
			}

			index = endIndex;
		}

		private static void ParseTypeParameterSymbol (string id, ref int index, IGenericParameterProvider? typeParameterContext, List<string> results)
		{
			// skip the first `
			Debug.Assert (PeekNextChar (id, index) == '`');
			index++;

			Debug.Assert (
				typeParameterContext == null ||
				(typeParameterContext is MethodDefinition && typeParameterContext.GenericParameterType == GenericParameterType.Method) ||
				(typeParameterContext is TypeDefinition && typeParameterContext.GenericParameterType == GenericParameterType.Type)
			);

			if (PeekNextChar (id, index) == '`') {
				// `` means this is a method type parameter
				index++;
				var methodTypeParameterIndex = ReadNextInteger (id, ref index);

				if (typeParameterContext is MethodDefinition methodContext) {
					var count = methodContext.HasGenericParameters ? methodContext.GenericParameters.Count : 0;
					if (count > 0 && methodTypeParameterIndex < count) {
						results.Add ($"``{methodTypeParameterIndex}");
					}
				}
			} else {
				// regular type parameter
				var typeParameterIndex = ReadNextInteger (id, ref index);

				var typeContext = typeParameterContext is MethodDefinition methodContext
					? methodContext.DeclaringType
					: typeParameterContext as TypeDefinition;

				if (typeParameterIndex >= 0 ||
					typeParameterIndex < typeContext?.GenericParameters.Count) {
					// No need to look at declaring types like Roslyn, because type parameters are redeclared.
					results.Add ("`" + typeParameterIndex);
				}
			}
		}

		private static void ParseNamedTypeSymbol (string id, ref int index, IGenericParameterProvider? typeParameterContext, List<string> results)
		{
			Debug.Assert (results.Count == 0);
			var nameBuilder = new StringBuilder ();
			// loop for dotted names
			while (true) {
				var name = ParseName (id, ref index);
				if (String.IsNullOrEmpty (name))
					return;

				nameBuilder.Append (name);

				List<string>? typeArguments = null;
				int arity = 0;

				// type arguments
				if (PeekNextChar (id, index) == '{') {
					typeArguments = new List<string> ();
					if (!ParseTypeArguments (id, ref index, typeParameterContext, typeArguments)) {
						continue;
					}

					arity = typeArguments.Count;
				}

				if (arity != 0) {
					Debug.Assert (typeArguments != null && typeArguments.Count != 0);
					nameBuilder.Append ('{');
					bool needsComma = false;
					foreach (var typeArg in typeArguments) {
						if (needsComma) {
							nameBuilder.Append (',');
						}
						nameBuilder.Append (typeArg);
						needsComma = true;
					}
					nameBuilder.Append ('}');
				}

				if (PeekNextChar (id, index) != '.')
					break;

				index++;
				nameBuilder.Append ('.');
			}

			results.Add (nameBuilder.ToString ());
		}

		private static int ParseArrayBounds (string id, ref int index)
		{
			index++; // skip '['

			int bounds = 0;

			while (true) {
				// note: the actual bounds are ignored.
				// C# only supports arrays with lower bound zero.
				// size is not known.

				if (char.IsDigit (PeekNextChar (id, index)))
					ReadNextInteger (id, ref index);

				if (PeekNextChar (id, index) == ':') {
					index++;

					// note: the spec says that omitting both the lower bounds and the size
					// should omit the ':' as well, but this allows for it in the input.
					if (char.IsDigit (PeekNextChar (id, index)))
						ReadNextInteger (id, ref index);
				}

				bounds++;

				if (PeekNextChar (id, index) == ',') {
					index++;
					continue;
				}

				break;
			}

			// note: this allows leaving out the closing ']'
			if (PeekNextChar (id, index) == ']')
				index++;

			return bounds;
		}

		private static bool ParseTypeArguments (string id, ref int index, IGenericParameterProvider? typeParameterContext, List<string> typeArguments)
		{
			index++; // skip over {

			while (true) {
				var type = ParseTypeSymbol (id, ref index, typeParameterContext);

				if (type == null) {
					// if a type argument cannot be identified, argument list is no good
					return false;
				}

				// add first one
				typeArguments.Add (type);

				if (PeekNextChar (id, index) == ',') {
					index++;
					continue;
				}

				break;
			}

			// note: this doesn't require closing }
			if (PeekNextChar (id, index) == '}') {
				index++;
			}

			return true;
		}

		private static void GetMatchingTypes (ModuleDefinition module, TypeDefinition? declaringType, string name, List<IMemberDefinition> results)
		{
			Debug.Assert (module != null);

			if (declaringType == null) {
				var type = module.GetType (name);
				if (type != null) {
					results.Add (type);
				}
				return;
			}

			if (!declaringType.HasNestedTypes)
				return;

			foreach (var nestedType in declaringType.NestedTypes) {
				Debug.Assert (String.IsNullOrEmpty (nestedType.Namespace));
				if (nestedType.Name != name)
					continue;
				results.Add (nestedType);
				return;
			}
		}

		private static void GetMatchingMethods (string id, ref int index, TypeDefinition? type, string memberName, int arity, List<IMemberDefinition> results)
		{
			if (type == null)
				return;

			var parameters = new List<string> ();
			var startIndex = index;
			var endIndex = index;

			foreach (var method in type.Methods) {
				index = startIndex;
				if (method.Name != memberName)
					continue;

				var methodArity = method.HasGenericParameters ? method.GenericParameters.Count : 0;
				if (methodArity != arity)
					continue;

				parameters.Clear ();
				if (PeekNextChar (id, index) == '(') {
					// if the parameters cannot be identified (some error), then the symbol cannot match, try next method symbol
					if (!ParseParameterList (id, ref index, method, parameters))
						continue;
				}

				if (!AllParametersMatch (method.Parameters, parameters))
					continue;

				if (PeekNextChar (id, index) == '~') {
					index++;
					string? returnType = ParseTypeSymbol (id, ref index, method);
					if (returnType == null)
						continue;

					// if return type is specified, then it must match
					if (method.ReturnType.GetSignaturePart () == returnType) {
						results.Add (method);
						endIndex = index;
					}
				} else {
					// no return type specified, then any matches
					results.Add (method);
					endIndex = index;
				}
			}
			index = endIndex;
		}

		private static void GetMatchingProperties (string id, ref int index, TypeDefinition? type, string memberName, List<IMemberDefinition> results)
		{
			if (type == null)
				return;

			int startIndex = index;
			int endIndex = index;

			List<string>? parameters = null;
			// Unlike Roslyn, we don't need to decode property names because we are working
			// directly with IL.
			foreach (var property in type.Properties) {
				index = startIndex;
				if (property.Name != memberName)
					continue;
				if (PeekNextChar (id, index) == '(') {
					if (parameters == null) {
						parameters = new List<string> ();
					} else {
						parameters.Clear ();
					}

					if (ParseParameterList (id, ref index, property.DeclaringType, parameters)
						&& AllParametersMatch (property.Parameters, parameters)) {
						results.Add (property);
						endIndex = index;
					}
				} else if (property.Parameters.Count == 0) {
					results.Add (property);
					endIndex = index;
				}
			}

			index = endIndex;
		}

		private static void GetMatchingFields (TypeDefinition? type, string memberName, List<IMemberDefinition> results)
		{
			if (type == null)
				return;
			foreach (var field in type.Fields) {
				if (field.Name != memberName)
					continue;
				results.Add (field);
			}
		}

		private static void GetMatchingEvents (TypeDefinition? type, string memberName, List<IMemberDefinition> results)
		{
			if (type == null)
				return;
			foreach (var evt in type.Events) {
				if (evt.Name != memberName)
					continue;
				results.Add (evt);
			}
		}

		private static bool AllParametersMatch (Collection<ParameterDefinition> methodParameters, List<string> expectedParameters)
		{
			if (methodParameters.Count != expectedParameters.Count)
				return false;

			for (int i = 0; i < expectedParameters.Count; i++) {
				if (methodParameters[i].ParameterType.GetSignaturePart () != expectedParameters[i])
					return false;
			}

			return true;
		}

		private static bool ParseParameterList (string id, ref int index, IGenericParameterProvider typeParameterContext, List<string> parameters)
		{
			System.Diagnostics.Debug.Assert (typeParameterContext != null);

			index++; // skip over '('

			if (PeekNextChar (id, index) == ')') {
				// note: this will match parameterless methods, or methods with only varargs parameters
				index++;
				return true;
			}

			string? parameter = ParseTypeSymbol (id, ref index, typeParameterContext);
			if (parameter == null)
				return false;

			parameters.Add (parameter);

			while (PeekNextChar (id, index) == ',') {
				index++;

				parameter = ParseTypeSymbol (id, ref index, typeParameterContext);
				if (parameter == null)
					return false;

				parameters.Add (parameter);
			}

			// note: this doesn't require the trailing ')'
			if (PeekNextChar (id, index) == ')') {
				index++;
			}

			return true;
		}

		private static char PeekNextChar (string id, int index)
		{
			return index >= id.Length ? '\0' : id[index];
		}

		private static readonly char[] s_nameDelimiters = { ':', '.', '(', ')', '{', '}', '[', ']', ',', '\'', '@', '*', '`', '~' };

		private static string ParseName (string id, ref int index)
		{
			string name;

			int delimiterOffset = id.IndexOfAny (s_nameDelimiters, index);
			if (delimiterOffset >= 0) {
				name = id.Substring (index, delimiterOffset - index);
				index = delimiterOffset;
			} else {
				name = id.Substring (index);
				index = id.Length;
			}

			return DecodeName (name);
		}

		// undoes dot encodings within names...
		private static string DecodeName (string name)
		{
			if (name.IndexOf ('#') >= 0)
				return name.Replace ('#', '.');

			return name;
		}

		private static int ReadNextInteger (string id, ref int index)
		{
			int n = 0;

			// note: this can overflow
			while (index < id.Length && char.IsDigit (id[index])) {
				n = n * 10 + (id[index] - '0');
				index++;
			}

			return n;
		}
	}
}