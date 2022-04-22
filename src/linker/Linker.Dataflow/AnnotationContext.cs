// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared.TypeSystemProxy;
using Mono.Linker;

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct AnnotationContext
	{
		readonly LinkContext _context;

		public AnnotationContext (LinkContext context) => _context = context;

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodReturnValue (method.Method.ReturnType.ResolveToTypeDefinition (_context), method.Method, dynamicallyAccessedMemberTypes);

		internal partial MethodReturnValue GetMethodReturnValue (MethodProxy method)
			=> GetMethodReturnValue (method, _context.Annotations.FlowAnnotations.GetReturnParameterAnnotation (method.Method));

#pragma warning disable CA1822 // Mark members as static - keep this an instance method for consistency with the others
		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new MethodThisParameterValue (method.Method, dynamicallyAccessedMemberTypes);
#pragma warning restore CA1822

		internal partial MethodThisParameterValue GetMethodThisParameterValue (MethodProxy method)
			=> GetMethodThisParameterValue (method, _context.Annotations.FlowAnnotations.GetParameterAnnotation (method.Method, 0));

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex, DynamicallyAccessedMemberTypes dynamicallyAccessedMemberTypes)
			=> new (method.Method.Parameters[parameterIndex].ParameterType.ResolveToTypeDefinition (_context), method.Method, parameterIndex, dynamicallyAccessedMemberTypes);

		internal partial MethodParameterValue GetMethodParameterValue (MethodProxy method, int parameterIndex)
			=> GetMethodParameterValue (method, parameterIndex, _context.Annotations.FlowAnnotations.GetParameterAnnotation (method.Method, parameterIndex + (method.IsStatic () ? 0 : 1)));
	}
}