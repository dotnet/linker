﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class DynamicallyAccessedMembersAnalyzer : DiagnosticAnalyzer
	{
		internal const string DynamicallyAccessedMembers = nameof (DynamicallyAccessedMembers);
		internal const string DynamicallyAccessedMembersAttribute = nameof (DynamicallyAccessedMembersAttribute);

		static ImmutableArray<DiagnosticDescriptor> GetSupportedDiagnostics ()
		{
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor>(23);
			for (int i = (int)DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter; 
				i <= (int)DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter; i++) {
				diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
			}

			return diagDescriptorsArrayBuilder.ToImmutable();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationAction (operationAnalysisContext => {
				var assignmentOperation = (IAssignmentOperation) operationAnalysisContext.Operation;
				ProcessAssignmentOperation (operationAnalysisContext, assignmentOperation);
			}, OperationKind.SimpleAssignment);

			context.RegisterOperationAction (operationAnalysisContext => {
				var invocationOperation = (IInvocationOperation) operationAnalysisContext.Operation;
				ProcessInvocationOperation (operationAnalysisContext, invocationOperation);
			}, OperationKind.Invocation);

			context.RegisterOperationAction (operationAnalysisContext => {
				var returnOperation = (IReturnOperation) operationAnalysisContext.Operation;
				ProcessReturnOperation (operationAnalysisContext, returnOperation);
			}, OperationKind.Return);
		}

		static void ProcessAssignmentOperation (OperationAnalysisContext context, IAssignmentOperation assignmentOperation)
		{
			if (TryGetSymbolFromOperation (assignmentOperation.Target) is not ISymbol target ||
				TryGetSymbolFromOperation (assignmentOperation.Value) is not ISymbol source)
				return;

			if (!target.TryGetDynamicallyAccessedMemberTypes (out var damtOnTarget))
				return;

			source.TryGetDynamicallyAccessedMemberTypes (out var damtOnSource);
			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				return;

			var diag = GetDiagnosticId (source.Kind, target.Kind);
			var diagArgs = GetDiagnosticArguments (source.Kind == SymbolKind.NamedType ? context.ContainingSymbol : source, target, missingAnnotations);
			context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), assignmentOperation.Syntax.GetLocation (), diagArgs));
		}

		static void ProcessInvocationOperation (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			ProcessTypeArguments (context, invocationOperation);
			ProcessArguments (context, invocationOperation);
			if (!invocationOperation.TargetMethod.TryGetDynamicallyAccessedMemberTypes (out var damtOnCalledMethod))
				return;

			if (TryGetSymbolFromOperation (invocationOperation.Instance) is ISymbol instance &&
				!instance.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute)) {
				instance!.TryGetDynamicallyAccessedMemberTypes (out var damtOnCaller);
				if (Annotations.SourceHasRequiredAnnotations (damtOnCaller, damtOnCalledMethod, out var missingAnnotations))
					return;

				var diag = GetDiagnosticId (instance!.Kind, invocationOperation.TargetMethod.Kind);
				var diagArgs = GetDiagnosticArguments (instance.Kind == SymbolKind.NamedType ? context.ContainingSymbol : instance, invocationOperation.TargetMethod, missingAnnotations);
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), invocationOperation.Syntax.GetLocation (), diagArgs));
			}
		}

		static void ProcessReturnOperation (OperationAnalysisContext context, IReturnOperation returnOperation)
		{
			if (!context.ContainingSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out var damtOnReturnType) ||
				context.ContainingSymbol.HasAttribute (RequiresUnreferencedCodeAnalyzer.FullyQualifiedRequiresUnreferencedCodeAttribute))
				return;

			if (TryGetSymbolFromOperation (returnOperation) is not ISymbol returnedSymbol)
				return;

			returnedSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnReturnedValue);
			if (Annotations.SourceHasRequiredAnnotations (damtOnReturnedValue, damtOnReturnType, out var missingAnnotations))
				return;

			var diag = GetDiagnosticId (returnedSymbol.Kind, context.ContainingSymbol.Kind, true);
			var diagArgs = GetDiagnosticArguments (returnedSymbol.Kind == SymbolKind.NamedType ? context.ContainingSymbol : returnedSymbol, context.ContainingSymbol, missingAnnotations);
			context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), returnOperation.Syntax.GetLocation (), diagArgs));
		}

		static void ProcessArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			foreach (var argument in invocationOperation.Arguments) {
				var targetParameter = argument.Parameter;
				if (targetParameter is null || !targetParameter.TryGetDynamicallyAccessedMemberTypes (out var damtOnParameter))
					return;

				ISymbol sourceArgument = argument.Value.Kind == OperationKind.Conversion ? context.ContainingSymbol : TryGetSymbolFromOperation (argument.Value)!;
				if (sourceArgument is null)
					return;

				sourceArgument.TryGetDynamicallyAccessedMemberTypes (out var damtOnArgument);
				if (Annotations.SourceHasRequiredAnnotations (damtOnArgument, damtOnParameter, out var missingAnnotations))
					return;

				var diag = GetDiagnosticId (sourceArgument.Kind, targetParameter.Kind, argument.Value.Kind != OperationKind.Conversion);
				var diagArgs = GetDiagnosticArguments (sourceArgument, targetParameter, missingAnnotations);
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), argument.Syntax.GetLocation (), diagArgs));
			}
		}

		static void ProcessTypeArguments (OperationAnalysisContext context, IInvocationOperation invocationOperation)
		{
			var targetMethod = invocationOperation.TargetMethod;
			for (int i = 0; i < targetMethod.TypeParameters.Length; i++) {
				var arg = targetMethod.TypeArguments[i];
				var param = targetMethod.TypeParameters[i];
				if (!param.TryGetDynamicallyAccessedMemberTypes (out var damtOnTypeParameter))
					continue;

				arg.TryGetDynamicallyAccessedMemberTypes (out var damtOnTypeArgument);
				if (Annotations.SourceHasRequiredAnnotations (damtOnTypeArgument, damtOnTypeParameter, out var missingAnnotations))
					continue;

				var diag = GetDiagnosticId (arg.Kind, param.Kind);
				var diagArgs = GetDiagnosticArguments (arg, param, missingAnnotations);
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), invocationOperation.Syntax.GetLocation (), diagArgs));
			}
		}

		static DiagnosticId GetDiagnosticId (SymbolKind source, SymbolKind target, bool targetsType = false)
			=> (source, target) switch {
				(SymbolKind.Parameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
				(SymbolKind.Parameter, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
				(SymbolKind.Parameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(SymbolKind.Field, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(SymbolKind.Field, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(SymbolKind.Field, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
				(SymbolKind.Field, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(SymbolKind.NamedType, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter,
				(SymbolKind.Method, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
				(SymbolKind.Method, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
				(SymbolKind.Method, SymbolKind.Parameter) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter :
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
				(SymbolKind.NamedType, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
				(SymbolKind.TypeParameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsField,
				(SymbolKind.TypeParameter, SymbolKind.Method) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsMethodReturnType,
				(SymbolKind.TypeParameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsParameter,
				(SymbolKind.TypeParameter, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter,
				_ => throw new NotImplementedException ()
			};

		static string[] GetDiagnosticArguments (ISymbol source, ISymbol target, string missingAnnotations)
		{
			var args = new List<string> ();
			args.AddRange (GetDiagnosticArguments (target));
			args.AddRange (GetDiagnosticArguments (source));
			args.Add (missingAnnotations);
			return args.ToArray ();
		}

		static IEnumerable<string> GetDiagnosticArguments (ISymbol symbol)
		{
			var args = new List<string> ();
			args.AddRange (symbol.Kind switch {
				SymbolKind.Parameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				SymbolKind.NamedType => new string[] { symbol.GetDisplayName () },
				SymbolKind.Field => new string[] { symbol.GetDisplayName () },
				SymbolKind.Method => new string[] { symbol.GetDisplayName () },
				SymbolKind.TypeParameter => new string[] { symbol.GetDisplayName (), symbol.ContainingSymbol.GetDisplayName () },
				_ => throw new NotImplementedException ($"Unsupported source or target symbol {symbol}.")
			});

			return args;
		}

		static ISymbol? TryGetSymbolFromOperation (IOperation? operation) =>
			operation switch {
				IArgumentOperation argument => TryGetSymbolFromOperation (argument.Value),
				IAssignmentOperation assignment => TryGetSymbolFromOperation (assignment.Value),
				IConversionOperation conversion => conversion.Type,
				IInstanceReferenceOperation instanceReference => instanceReference.Type,
				IInvocationOperation invocation => invocation.TargetMethod,
				IMemberReferenceOperation memberReference => memberReference.Member,
				IParameterReferenceOperation parameterReference => parameterReference.Parameter,
				IReturnOperation returnOp => TryGetSymbolFromOperation (returnOp.ReturnedValue),
				ITypeOfOperation typeOf => typeOf.TypeOperand,
				_ => null
			};
	}
}
