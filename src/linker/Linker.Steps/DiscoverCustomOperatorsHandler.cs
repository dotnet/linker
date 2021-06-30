// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class DiscoverOperatorsHandler : IMarkHandler
	{
		LinkContext _context;
		bool markOperators;
		HashSet<TypeDefinition> _trackedTypesWithOperators;
		Dictionary<TypeDefinition, HashSet<MethodDefinition>> _pendingOperatorsForType;

		public void Initialize (LinkContext context, MarkContext markContext)
		{
			_context = context;
			_trackedTypesWithOperators = new HashSet<TypeDefinition> ();
			_pendingOperatorsForType = new Dictionary<TypeDefinition, HashSet<MethodDefinition>> ();
			markContext.RegisterMarkTypeAction (ProcessType);
		}

		void ProcessType (TypeDefinition type)
		{
			CheckForLinqExpressions (type);

			if (_pendingOperatorsForType.TryGetValue (type, out var pendingOperators)) {
				foreach (var customOperator in pendingOperators)
					MarkOperator (customOperator);
				_pendingOperatorsForType.Remove (type);
			}

			if (ProcessCustomOperators (type, mark: markOperators) && !markOperators)
				_trackedTypesWithOperators.Add (type);
		}

		void CheckForLinqExpressions (TypeDefinition type)
		{
			if (markOperators)
				return;

			if (type.Namespace != "System.Linq.Expressions" || type.Name != "Expression")
				return;

			markOperators = true;

			foreach (var markedType in _trackedTypesWithOperators)
				ProcessCustomOperators (markedType, mark: true);

			_trackedTypesWithOperators.Clear ();
		}

		void MarkOperator (MethodDefinition method)
		{
			_context.Annotations.Mark (method, new DependencyInfo (DependencyKind.PreservedOperator, method.DeclaringType));
		}

		bool ProcessCustomOperators (TypeDefinition type, bool mark)
		{
			if (!type.HasMethods)
				return false;

			bool hasCustomOperators = false;
			foreach (var method in type.Methods) {
				if (!IsOperator (method, out var otherType))
					continue;

				if (!mark)
					return true;

				hasCustomOperators = true;

				if (otherType == null || _context.Annotations.IsMarked (otherType)) {
					MarkOperator (method);
					continue;
				}

				// Wait until otherType gets marked to mark the operator.
				if (!_pendingOperatorsForType.TryGetValue (otherType, out var pendingOperators)) {
					pendingOperators = new HashSet<MethodDefinition> ();
					_pendingOperatorsForType.Add (otherType, pendingOperators);
				}
				pendingOperators.Add (method);
			}
			return hasCustomOperators;
		}

		TypeDefinition _int32;
		TypeDefinition Int32 {
			get {
				if (_int32 == null)
					_int32 = BCL.FindPredefinedType ("System", "Int32", _context);
				return _int32;
			}
		}

		bool IsOperator (MethodDefinition method, out TypeDefinition otherType)
		{
			otherType = null;

			if (!method.IsStatic || !method.IsPublic || !method.IsSpecialName || !method.Name.StartsWith ("op_"))
				return false;

			var operatorName = method.Name.Substring (3);
			var self = method.DeclaringType;

			switch (operatorName) {
			// Unary operators
			case "UnaryPlus":
			case "UnaryNegation":
			case "LogicalNot":
			case "OnesComplement":
			case "Increment":
			case "Decrement":
			case "True":
			case "False":
				// Parameter type of a unary operator must be the declaring type
				if (method.Parameters.Count != 1 || _context.TryResolve (method.Parameters[0].ParameterType) != self)
					return false;
				// ++ and -- must return the declaring type
				if (operatorName is "Increment" or "Decrement" && _context.TryResolve (method.ReturnType) != self)
					return false;
				return true;
			// Binary operators
			case "Addition":
			case "Subtraction":
			case "Multiply":
			case "Division":
			case "Modulus":
			case "BitwiseAnd":
			case "BitwiseOr":
			case "ExclusiveOr":
			// take int as right
			case "LeftShift":
			case "RightShift":
			case "Equality":
			case "Inequality":
			case "LessThan":
			case "GreaterThan":
			case "LessThanOrEqual":
			case "GreaterThanOrEqual":
				if (method.Parameters.Count != 2)
					return false;
				var left = _context.TryResolve (method.Parameters[0].ParameterType);
				var right = _context.TryResolve (method.Parameters[1].ParameterType);
				if (left == null || right == null)
					return false;
				// << and >> must take the declaring type and int
				if (operatorName is "LeftShift" or "RightShift" && (left != self || right != Int32))
					return false;
				// At least one argument must be the declaring type
				if (left != self && right != self)
					return false;
				if (left != self)
					otherType = left;
				if (right != self)
					otherType = right;
				return true;
			// Conversion operators
			case "Implicit":
			case "Explicit":
				if (method.Parameters.Count != 1)
					return false;
				var source = _context.TryResolve (method.Parameters[0].ParameterType);
				var target = _context.TryResolve (method.ReturnType);
				if (source == null || target == null)
					return false;
				// Exactly one of source/target must be the declaring type
				if (source == self == (target == self))
					return false;
				otherType = source == self ? target : source;
				return true;
			default:
				return false;
			}
		}
	}
}