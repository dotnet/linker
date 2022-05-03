// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;

using MultiValue = ILLink.Shared.DataFlow.ValueSet<ILLink.Shared.DataFlow.SingleValue>;

namespace Mono.Linker.Dataflow
{
	public readonly record struct TrimAnalysisMethodCallPattern
	{
		public readonly MethodReference CalledMethod;
		public readonly MultiValue Instance;
		public readonly ImmutableArray<MultiValue> Arguments;
		public readonly MessageOrigin Origin;

		public TrimAnalysisMethodCallPattern (
			MethodReference calledMethod,
			MultiValue instance,
			ImmutableArray<MultiValue> arguments,
			MessageOrigin origin)
		{
			Debug.Assert (origin.Provider is MethodDefinition);
			CalledMethod = calledMethod;
			Instance = instance.Clone ();
			if (arguments.IsEmpty) {
				Arguments = ImmutableArray<MultiValue>.Empty;
			} else {
				var builder = ImmutableArray.CreateBuilder<MultiValue> ();
				foreach (var argument in arguments)
					builder.Add (argument.Clone ());
				Arguments = builder.ToImmutableArray ();
			}
			Origin = origin;
		}

		public void MarkAndProduceDiagnostics (bool diagnosticsEnabled, LinkContext context, ReflectionMarker reflectionMarker, MarkStep markStep)
		{
			var diagnosticContext = new DiagnosticContext (Origin, diagnosticsEnabled, context);
			ReflectionMethodBodyScanner.HandleCall (CalledMethod, Instance, Arguments,
				diagnosticContext,
				reflectionMarker,
				context,
				context.Annotations.FlowAnnotations,
				out MultiValue _,
				markStep);
		}
	}
}