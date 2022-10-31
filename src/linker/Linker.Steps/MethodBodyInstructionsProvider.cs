// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;

namespace Mono.Linker.Steps
{
	public class MethodBodyInstructionsProvider
	{
		readonly UnreachableBlocksOptimizer _unreachableBlocksOptimizer;

		public readonly struct ProcessedMethodBody
		{
			private ProcessedMethodBody (MethodBody body) => this.Body = body;

			public readonly MethodBody Body;

			public MethodDefinition Method => Body.Method;

#pragma warning disable RS0030 // Wrapper which provides safe access to the property
			public Collection<Instruction> Instructions => Body.Instructions;
#pragma warning restore RS0030

#pragma warning disable RS0030 // Wrapper which provides safe access to the property
			public Collection<ExceptionHandler> ExceptionHandlers => Body.ExceptionHandlers;
#pragma warning restore RS0030

#pragma warning disable RS0030 // Wrapper which provides safe access to the property
			public Collection<VariableDefinition> Variables => Body.Variables;
#pragma warning restore RS0030

			public static ProcessedMethodBody Create (MethodBody body) => new ProcessedMethodBody (body);
		}

		public MethodBodyInstructionsProvider(LinkContext context)
		{
			_unreachableBlocksOptimizer = new UnreachableBlocksOptimizer(context);
		}

		public ProcessedMethodBody GetMethodBody (MethodBody methodBody)
			=> GetMethodBody (methodBody.Method);

		public ProcessedMethodBody GetMethodBody (MethodDefinition method)
		{
			_unreachableBlocksOptimizer.ProcessMethod (method);

			return ProcessedMethodBody.Create (method.Body);
		}
	}
}
