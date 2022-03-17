// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker
{
	// Currently this is implemented using heuristics
	public class CompilerGeneratedState
	{
		readonly LinkContext _context;
		readonly Dictionary<TypeDefinition, MethodDefinition> _compilerGeneratedTypeToUserCodeMethod;
		// TODO: fix accessibility
		internal readonly Dictionary<MethodDefinition, MethodDefinition> _compilerGeneratedMethodToUserCodeMethod;
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


		public void TrackCallToLambdaOrLocalFunction (MethodDefinition caller, MethodDefinition lambdaOrLocalFunction)
		{
			// The declaring type check makes sure we don't treat MoveNext as a normal method. It should be treated as compiler-generated,
			// mapping to the state machine user method. TODO: check that this doesn't cause problems for global type, etc.
			bool callerIsStateMachineMethod = GeneratedNames.IsGeneratedMemberName (caller.DeclaringType.Name) && !GeneratedNames.IsLambdaDisplayClass (caller.DeclaringType.Name);
			if (callerIsStateMachineMethod)
				return;

			bool callerIsLambdaOrLocal = GeneratedNames.IsGeneratedMemberName (caller.Name) && !callerIsStateMachineMethod;

			if (!callerIsLambdaOrLocal) {
				// Caller is a normal method...
				bool added = _compilerGeneratedMethodToUserCodeMethod.TryAdd (lambdaOrLocalFunction, caller);
				// There should only be one non-compiler-generated caller of a lambda or local function.
				Debug.Assert (added || _compilerGeneratedMethodToUserCodeMethod[lambdaOrLocalFunction] == caller);
				return;
			}

			Debug.Assert (GeneratedNames.TryParseLambdaMethodName (caller.Name, out _) || GeneratedNames.TryParseLocalFunctionMethodName (caller.Name, out _, out _));
			// Caller is a lambda or local function. This means the lambda or local function is contained within the scope of the caller's user-defined method.

			if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (caller, out MethodDefinition? userCodeMethod)) {
				// This lambda/localfn is in the same user code as the caller.
				bool added = _compilerGeneratedMethodToUserCodeMethod.TryAdd (lambdaOrLocalFunction, userCodeMethod);
				Debug.Assert (added || _compilerGeneratedMethodToUserCodeMethod[lambdaOrLocalFunction] == caller);
			} else {
				// Haven't tracked any calls to the caller yet.
				throw new System.Exception ("Not yet handled! Need to postpone marking of such methods until we can identify a caller, or bail out.");
			}
		}

		void PopulateCacheForType (TypeDefinition type)
		{
			// Avoid repeat scans of the same type
			if (!_typesWithPopulatedCache.Add (type))
				return;

			Dictionary<string, List<MethodDefinition>>? lambdaMethods = null;
			Dictionary<string, List<MethodDefinition>>? localFunctions = null;

			foreach (TypeDefinition nested in type.NestedTypes) {
				if (!GeneratedNames.IsLambdaDisplayClass (nested.Name))
					continue;

				// Lambdas and local functions may be generated into a display class which holds
				// the closure environment. Lambdas are always generated into such a class.

				// Local functions typically get emitted outside of the
				// display class (which is a struct in this case), but when any of the captured state
				// is used by a state machine local function, the local function is emitted into a
				// display class holding that captured state.
				lambdaMethods ??= new Dictionary<string, List<MethodDefinition>> ();
				localFunctions ??= new Dictionary<string, List<MethodDefinition>> ();

				foreach (var lambdaMethod in nested.Methods) {
					if (!GeneratedNames.TryParseLambdaMethodName (lambdaMethod.Name, out string? userMethodName))
						continue;
					if (!lambdaMethods.TryGetValue (userMethodName, out List<MethodDefinition>? lambdaMethodsForName)) {
						lambdaMethodsForName = new List<MethodDefinition> ();
						lambdaMethods.Add (userMethodName, lambdaMethodsForName);
					}
					lambdaMethodsForName.Add (lambdaMethod);
				}

				foreach (var localFunction in nested.Methods) {
					if (!GeneratedNames.TryParseLocalFunctionMethodName (localFunction.Name, out string? userMethodName, out string? localFunctionName))
						continue;
					if (!localFunctions.TryGetValue (userMethodName, out List<MethodDefinition>? localFunctionsForName)) {
						localFunctionsForName = new List<MethodDefinition> ();
						localFunctions.Add (userMethodName, localFunctionsForName);
					}
					localFunctionsForName.Add (localFunction);
				}
			}


			foreach (MethodDefinition localFunction in type.Methods) {
				if (!GeneratedNames.TryParseLocalFunctionMethodName (localFunction.Name, out string? userMethodName, out string? localFunctionName))
					continue;

				// Local functions may be generated into the same type as its declaring method,
				// alongside a displayclass which holds the captured state.
				// Or it may not have a displayclass, if there is no captured state.

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
						// TODO: change back to add
						_compilerGeneratedMethodToUserCodeMethod.TryAdd (lambdaMethod, method);
				}

				if (localFunctions?.TryGetValue (method.Name, out List<MethodDefinition>? localFunctionsForName) == true) {
					foreach (var localFunction in localFunctionsForName)
						// TODO: change back to add
						_compilerGeneratedMethodToUserCodeMethod.TryAdd (localFunction, method);
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
						if (stateMachineType == null)
							break;
						Debug.Assert (stateMachineType.DeclaringType == type);
						if (!_compilerGeneratedTypeToUserCodeMethod.TryAdd (stateMachineType, method)) {
							var alreadyAssociatedMethod = _compilerGeneratedTypeToUserCodeMethod[stateMachineType];
							_context.LogWarning (new MessageOrigin (method), DiagnosticId.MethodsAreAssociatedWithStateMachine, method.GetDisplayName (), alreadyAssociatedMethod.GetDisplayName (), stateMachineType.GetDisplayName ());
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

		public bool TryGetOwningMethodForCompilerGeneratedMember (IMemberDefinition sourceMember, [NotNullWhen (true)] out MethodDefinition? owningMethod)
		{
			owningMethod = null;
			if (sourceMember == null)
				return false;

			MethodDefinition? compilerGeneratedMethod = sourceMember as MethodDefinition;
			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out owningMethod))
					return true;
			}

			TypeDefinition sourceType = (sourceMember as TypeDefinition) ?? sourceMember.DeclaringType;

			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (sourceType, out owningMethod))
				return true;

			if (sourceType.DeclaringType == null)
				return false;

			var typeToCache = HasRoslynCompilerGeneratedName (sourceType) ? sourceType.DeclaringType : sourceType;

			// Now go to its declaring type and search all methods to find the one which points to the type as its
			// state machine implementation.
			PopulateCacheForType (typeToCache);
			if (compilerGeneratedMethod != null) {
				if (_compilerGeneratedMethodToUserCodeMethod.TryGetValue (compilerGeneratedMethod, out owningMethod))
					return true;
			}

			if (_compilerGeneratedTypeToUserCodeMethod.TryGetValue (sourceType, out owningMethod))
				return true;

			return false;
		}
	}
}
