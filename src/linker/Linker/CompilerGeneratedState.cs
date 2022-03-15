// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	// Currently this is implemented using heuristics
	public class CompilerGeneratedState
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, MethodDefinition> _compilerGeneratedTypeToUserCodeMethod;
		readonly Dictionary<MethodDefinition, MethodDefinition> _compilerGeneratedMethodToUserCodeMethod;
		readonly HashSet<TypeDefinition> _typesWithPopulatedCache;

		public CompilerGeneratedState (LinkContext context)
		{
			_context = context;
			_compilerGeneratedTypeToUserCodeMethod = new Dictionary<TypeDefinition, MethodDefinition> ();
			_compilerGeneratedMethodToUserCodeMethod = new Dictionary<MethodDefinition, MethodDefinition> ();
			_typesWithPopulatedCache = new HashSet<TypeDefinition> ();
		}

		static bool HasRoslynCompilerGeneratedName (TypeDefinition type) =>
			GeneratedNames.IsGeneratedMemberName (type.Name) || (type.DeclaringType != null && HasRoslynCompilerGeneratedName (type.DeclaringType));

		void PopulateCacheForType (TypeDefinition type)
		{
			// Avoid repeat scans of the same type
			if (!_typesWithPopulatedCache.Add (type))
				return;

			Dictionary<string, List<MethodDefinition>>? lambdaMethods = null;

			foreach (TypeDefinition nested in type.NestedTypes) {
				if (!GeneratedNames.IsLambdaDisplayClass (nested.Name))
					continue;

				lambdaMethods ??= new Dictionary<string, List<MethodDefinition>> ();

				foreach (var lambdaMethod in nested.Methods) {
					if (!GeneratedNames.TryParseLambdaMethodName (lambdaMethod.Name, out string? userMethodName))
						continue;
					if (!lambdaMethods.TryGetValue (userMethodName, out List<MethodDefinition>? lambdaMethodsForName)) {
						lambdaMethodsForName = new List<MethodDefinition> ();
						lambdaMethods.Add (userMethodName, lambdaMethodsForName);
					}
					lambdaMethodsForName.Add (lambdaMethod);
				}
			}

			Dictionary<string, List<MethodDefinition>>? localFunctions = null;

			foreach (MethodDefinition localFunction in type.Methods) {
				if (!GeneratedNames.TryParseLocalFunctionMethodName (localFunction.Name, out string? userMethodName, out string? localFunctionName))
					continue;

				localFunctions ??= new Dictionary<string, List<MethodDefinition>> ();

				if (!localFunctions.TryGetValue (userMethodName, out List<MethodDefinition>? localFunctionsForName)) {
					localFunctionsForName = new List<MethodDefinition> ();
					localFunctions.Add (userMethodName, localFunctionsForName);
				}
				localFunctionsForName.Add (localFunction);
			}

			foreach (MethodDefinition method in type.Methods) {
				// TODO: combine into one thing?

				if (lambdaMethods?.TryGetValue (method.Name, out List<MethodDefinition>? lambdaMethodsForName) == true) {
					foreach (var lambdaMethod in lambdaMethodsForName)
						_compilerGeneratedMethodToUserCodeMethod.Add (lambdaMethod, method);
				}

				if (localFunctions?.TryGetValue (method.Name, out List<MethodDefinition>? localFunctionsForName) == true) {
					foreach (var localFunction in localFunctionsForName)
						_compilerGeneratedMethodToUserCodeMethod.Add (localFunction, method);
				}

				if (!method.HasCustomAttributes)
					continue;

				foreach (var attribute in method.CustomAttributes) {
					if (attribute.AttributeType.Namespace != "System.Runtime.CompilerServices")
						continue;

					switch (attribute.AttributeType.Name) {
					case "AsyncIteratorStateMachineAttribute":
					case "AsyncStateMachineAttribute":
					case "IteratorStateMachineAttribute":
						TypeDefinition? stateMachineType = GetFirstConstructorArgumentAsType (attribute);
						if (stateMachineType != null) {
							if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd (stateMachineType, method)) {
								var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
								_context.LogWarning (new MessageOrigin (method), DiagnosticId.MethodsAreAssociatedWithStateMachine, method.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), stateMachineType.GetDisplayName ());
							}
						}

						break;
					}
				}
			}
		}

		static TypeDefinition? GetFirstConstructorArgumentAsType (CustomAttribute attribute)
		{
			if (!attribute.HasConstructorArguments)
				return null;

			return attribute.ConstructorArguments[0].Value as TypeDefinition;
		}

		public MethodDefinition? GetUserDefinedMethodForCompilerGeneratedMember (IMemberDefinition sourceMember)
		{
			if (sourceMember == null)
				return null;

			MethodDefinition? userDefinedMethod;
			MethodDefinition? compilerGeneratedMethod = sourceMember as MethodDefinition;
			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out userDefinedMethod))
					return userDefinedMethod;
			}

			TypeDefinition compilerGeneratedType = (sourceMember as TypeDefinition) ?? sourceMember.DeclaringType;
			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (compilerGeneratedType, out userDefinedMethod))
				return userDefinedMethod;

			// Only handle async or iterator state machine
			// So go to the declaring type and check if it's compiler generated (as a perf optimization)
			if (!HasRoslynCompilerGeneratedName (compilerGeneratedType) || compilerGeneratedType.DeclaringType == null)
				return null;

			// Now go to its declaring type and search all methods to find the one which points to the type as its
			// state machine implementation.
			PopulateCacheForType (compilerGeneratedType.DeclaringType);
			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out userDefinedMethod))
					return userDefinedMethod;
			}

			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (compilerGeneratedType, out userDefinedMethod))
				return userDefinedMethod;

			return null;
		}
	}
}
