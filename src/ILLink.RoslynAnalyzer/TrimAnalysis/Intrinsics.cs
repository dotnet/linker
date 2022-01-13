// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using ILLink.RoslynAnalyzer.TrimAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Microsoft.CodeAnalysis;

namespace ILLink.Shared.TrimAnalysis
{
	partial struct Intrinsics
	{
		readonly IOperation _operation;

		public Intrinsics (IOperation operation) => _operation = operation;

		// TODO: This is relatively expensive on the analyzer since it doesn't cache the annotation information
		// In linker this is an optimization to avoid the heavy lifting of analysis if there's no point
		// it's unclear if the same optimization makes sense for the analyzer.
		private partial bool MethodRequiresDataFlowAnalysis (MethodProxy method) => FlowAnnotations.RequiresDataFlowAnalysis (method.Method);

		private partial DynamicallyAccessedMemberTypes GetReturnValueAnnotation (MethodProxy method) => FlowAnnotations.GetMethodReturnValueAnnotation (method.Method);

		private partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method, dynamicallyAccessedMemberTypes);

		private partial MethodProxy GetCallingMethod ()
		{
			var operation = _operation?.Parent;
			while (operation != null) {

				operation = operation.Parent;
			}

			return default;
		}
	}
}
