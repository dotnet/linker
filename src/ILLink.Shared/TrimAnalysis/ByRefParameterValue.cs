// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.DataFlow;
using ILLink.Shared.TypeSystemProxy;

namespace ILLink.Shared.TrimAnalysis
{
	sealed record ByRefParameterValue : ValueWithDynamicallyAccessedMembers
	{
		/// <summary>
		/// Constructs a ByRefParameterValue
		/// </summary>
		/// <param name="method"></param>
		/// <param name="index">This corresponds to the index of the parameter in code (i.e. 0 is the first parameter, not the `this` parameter)</param>
		/// <param name="dynamicallyAccessedMemberTypes"></param>
		public ByRefParameterValue (MethodProxy method, int index, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
		{
			DynamicallyAccessedMemberTypes = dynamicallyAccessedMemberTypes;
			Index = index;
			DeclaringMethod = method;
		}
		public override SingleValue DeepCopy () => this;
		public override DynamicallyAccessedMemberTypes DynamicallyAccessedMemberTypes { get; }
		public int Index { get; }
		public MethodProxy DeclaringMethod { get; }

		public override IEnumerable<string> GetDiagnosticArgumentsForAnnotationMismatch ()
		{
			var args = new List<string> {
				DeclaringMethod.Method.Parameters[Index].Name,
				DeclaringMethod.GetDisplayName ()
			};
			return args;
		}
	}
}