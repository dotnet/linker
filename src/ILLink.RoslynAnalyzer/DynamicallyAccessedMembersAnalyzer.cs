// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
			var diagDescriptorsList = new List<DiagnosticDescriptor> ();
			for (int i = 2067; i <= 2091; i++)
				diagDescriptorsList.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));

			return diagDescriptorsList.ToImmutableArray ();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationAction (DynamicallyAccessedMembersAnalyze,
				OperationKind.Invocation,
				OperationKind.Return,
				OperationKind.SimpleAssignment);
		}

		void DynamicallyAccessedMembersAnalyze (OperationAnalysisContext context)
		{
			switch (context.Operation) {
			case IAssignmentOperation assignmentOp:
				ProcessAssignmentOperation (context, assignmentOp);
				break;

			case IInvocationOperation invocationOp:
				ProcessInvocationOperation (context, invocationOp);
				break;

			case IReturnOperation returnOp:
				ProcessReturnOperation (context, returnOp);
				break;

			default:
				break;
			}
		}

		static void ProcessAssignmentOperation (OperationAnalysisContext context, IAssignmentOperation assignmentOperation)
		{
			if (GetSymbolFromOperation (assignmentOperation.Target) is not ISymbol target ||
				GetSymbolFromOperation (assignmentOperation.Value) is not ISymbol source)
				return;

			if (!target.TryGetDynamicallyAccessedMemberTypes (out var damtOnTarget))
				return;

			source.TryGetDynamicallyAccessedMemberTypes (out var damtOnSource);
			if (SourceHasMatchingAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
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

			if (GetSymbolFromOperation (invocationOperation.Instance) is ISymbol instance) {
				instance!.TryGetDynamicallyAccessedMemberTypes (out var damtOnCaller);
				if (SourceHasMatchingAnnotations (damtOnCaller, damtOnCalledMethod, out var missingAnnotations))
					return;

				var diag = GetDiagnosticId (instance!.Kind, invocationOperation.TargetMethod.Kind);
				var diagArgs = GetDiagnosticArguments (instance.Kind == SymbolKind.NamedType ? context.ContainingSymbol : instance, invocationOperation.TargetMethod, missingAnnotations);
				context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), invocationOperation.Syntax.GetLocation (), diagArgs));
			}
		}

		static void ProcessReturnOperation (OperationAnalysisContext context, IReturnOperation returnOperation)
		{
			if (!context.ContainingSymbol.TryGetDynamicallyAccessedMemberTypesOnReturnType (out var damtOnReturnType))
				return;

			if (GetSymbolFromOperation (returnOperation) is not ISymbol returnedSymbol)
				return;

			returnedSymbol.TryGetDynamicallyAccessedMemberTypes (out var damtOnReturnedValue);
			if (SourceHasMatchingAnnotations (damtOnReturnedValue, damtOnReturnType, out var missingAnnotations))
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

				ISymbol sourceArgument = argument.Value.Kind == OperationKind.Conversion ? context.ContainingSymbol : GetSymbolFromOperation (argument.Value)!;
				if (sourceArgument is null)
					return;

				sourceArgument.TryGetDynamicallyAccessedMemberTypes (out var damtOnArgument);
				if (SourceHasMatchingAnnotations (damtOnArgument, damtOnParameter, out var missingAnnotations))
					return;

				var diag = GetDiagnosticId (sourceArgument.Kind, targetParameter.Kind, argument.Value.Kind == OperationKind.Conversion);
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
				if (SourceHasMatchingAnnotations (damtOnTypeArgument, damtOnTypeParameter, out var missingAnnotations))
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
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethod,
				(SymbolKind.Parameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(SymbolKind.Field, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(SymbolKind.Field, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(SymbolKind.Field, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethod,
				(SymbolKind.Field, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(SymbolKind.NamedType, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodTargetsMethod,
				(SymbolKind.Method, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
				(SymbolKind.Method, SymbolKind.Method) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethod,
				(SymbolKind.Method, SymbolKind.Parameter) => targetsType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodTargetsParameter :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter,
				(SymbolKind.NamedType, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodTargetsField,
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

		static ISymbol? GetSymbolFromOperation (IOperation? operation) =>
			operation switch {
				IArgumentOperation argument => GetSymbolFromOperation (argument.Value),
				IAssignmentOperation assignment => GetSymbolFromOperation (assignment.Value),
				IConversionOperation conversion => conversion.Type,
				IInstanceReferenceOperation instanceReference => instanceReference.Type,
				IInvocationOperation invocation => invocation.TargetMethod,
				IMemberReferenceOperation memberReference => memberReference.Member,
				IParameterReferenceOperation parameterReference => parameterReference.Parameter,
				IReturnOperation returnOp => GetSymbolFromOperation (returnOp.ReturnedValue),
				ITypeOfOperation typeOf => typeOf.TypeOperand,
				_ => null
			};

		static bool SourceHasMatchingAnnotations (
			DynamicallyAccessedMemberTypes? sourceMemberTypes,
			DynamicallyAccessedMemberTypes? targetMemberTypes,
			out string missingMemberTypesString)
		{
			missingMemberTypesString = $"'{nameof (DynamicallyAccessedMemberTypes.All)}'";
			if (targetMemberTypes == null)
				return true;

			sourceMemberTypes ??= DynamicallyAccessedMemberTypes.None;
			var missingMemberTypesList = Enum.GetValues (typeof (DynamicallyAccessedMemberTypes))
				.Cast<DynamicallyAccessedMemberTypes> ()
				.Where (damt => (damt & targetMemberTypes & ~sourceMemberTypes) == damt && damt != DynamicallyAccessedMemberTypes.None)
				.ToList ();

			if (missingMemberTypesList.Count == 0)
				return true;

			if (missingMemberTypesList.Contains (DynamicallyAccessedMemberTypes.PublicConstructors) &&
				missingMemberTypesList.SingleOrDefault (mt => mt == DynamicallyAccessedMemberTypes.PublicParameterlessConstructor) is var ppc &&
				ppc != DynamicallyAccessedMemberTypes.None)
				missingMemberTypesList.Remove (ppc);

			missingMemberTypesString = string.Join (", ", missingMemberTypesList.Select (mmt => $"'DynamicallyAccessedMemberTypes.{mmt}'"));
			return false;
		}
	}
}
