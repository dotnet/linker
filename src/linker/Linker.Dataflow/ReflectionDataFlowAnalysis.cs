// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using ILLink.Shared.TrimAnalysis;
using Mono.Cecil;
using Mono.Linker.Steps;

namespace Mono.Linker.Dataflow
{
	sealed class ReflectionDataFlowAnalysis : LocalDataFlowAnalysis<ReflectionScanner>
	{
		public TrimAnalysisPatternStore TrimAnalysisPatterns { get; }

		public ReflectionDataFlowAnalysis (LinkContext context)
			: base (context)
		{
			TrimAnalysisPatterns = new TrimAnalysisPatternStore (Lattice.LocalsLattice.ValueLattice, context);
		}

		public void AnalyzeMethod (MethodDefinition method, MarkStep parent, MessageOrigin origin)
		{
			if (TryAnalyzeMethod (method, parent, origin)) {
				var reflectionMarker = new ReflectionMarker (Context, parent, enabled: true);
				TrimAnalysisPatterns.MarkAndProduceDiagnostics (reflectionMarker, parent);
			} else {
				var scanner = new ReflectionMethodBodyScanner (Context, parent, origin, TrimAnalysisPatterns);
				scanner.InterproceduralScan (method.Body);
			}

		}

		protected override ReflectionScanner GetBodyScanner (
			LinkContext context, MarkStep parent, MessageOrigin origin)
		 => new (context, parent, origin, TrimAnalysisPatterns);


		public static void InterproceduralScan ()
		{

		}

		public static bool RequiresReflectionMethodBodyScannerForCallSite (LinkContext context, MethodReference calledMethod)
		{
			MethodDefinition? methodDefinition = context.TryResolve (calledMethod);
			if (methodDefinition == null)
				return false;

			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition) ||
				context.Annotations.DoesMethodRequireUnreferencedCode (methodDefinition, out _) ||
				methodDefinition.IsPInvokeImpl && ComHandler.ComDangerousMethod (methodDefinition, context);
		}

		public static bool RequiresReflectionMethodBodyScannerForMethodBody (LinkContext context, MethodDefinition methodDefinition)
		{
			return Intrinsics.GetIntrinsicIdForMethod (methodDefinition) > IntrinsicId.RequiresReflectionBodyScanner_Sentinel ||
				context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (methodDefinition);
		}

		public static bool RequiresReflectionMethodBodyScannerForAccess (LinkContext context, FieldReference field)
		{
			FieldDefinition? fieldDefinition = context.TryResolve (field);
			if (fieldDefinition == null)
				return false;

			return context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (fieldDefinition);
		}
	}
}
