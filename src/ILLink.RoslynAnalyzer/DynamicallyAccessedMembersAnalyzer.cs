// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.FlowAnalysis;
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
			var diagDescriptorsArrayBuilder = ImmutableArray.CreateBuilder<DiagnosticDescriptor> (23);
			for (int i = (int) DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter;
				i <= (int) DiagnosticId.DynamicallyAccessedMembersMismatchTypeArgumentTargetsGenericParameter; i++) {
				diagDescriptorsArrayBuilder.Add (DiagnosticDescriptors.GetDiagnosticDescriptor ((DiagnosticId) i));
			}

			return diagDescriptorsArrayBuilder.ToImmutable ();
		}

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => GetSupportedDiagnostics ();

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterOperationBlockAction (context => {
				foreach (var operationBlock in context.OperationBlocks) {
					ControlFlowGraph cfg = context.GetControlFlowGraph (operationBlock);
					DynamicallyAccessedMembersAnalysis damAnalysis = new (context, cfg);

					foreach (ReflectionAccessPattern accessPattern in damAnalysis.GetResults ())
						CheckAndReportDynamicallyAccessedMembers (accessPattern.Source, accessPattern.Target, context, accessPattern.Operation.Syntax.GetLocation ());
				}
			});
		}

		// static void CheckAndReportDynamicallyAccessedMembers (IOperation sourceOperation, IOperation targetOperation, OperationBlockAnalysisContext context, DynamicallyAccessedMembersAnalysis analysis, Location location)
		// {
		// 	if (TryGetSymbolFromOperation (targetOperation, context) is not ISymbol target ||
		// 		!TryGetSourceValue (sourceOperation, context, analysis, out HashSetWrapper<SingleValue> source))
		// 		return;

		// 	CheckAndReportDynamicallyAccessedMembers (source, target, context, location, targetIsMethodReturn: false);
		// }

		// static void CheckAndReportDynamicallyAccessedMembers (IOperation sourceOperation, ISymbol target, OperationBlockAnalysisContext context, DynamicallyAccessedMembersAnalysis analysis, Location location, bool targetIsMethodReturn)
		// {
		// 	if (!TryGetSourceValue (sourceOperation, context, analysis, out HashSetWrapper<SingleValue> source))
		// 		return;

		// 	CheckAndReportDynamicallyAccessedMembers (source, target, context, location, targetIsMethodReturn);
		// }

		static void CheckAndReportDynamicallyAccessedMembers (HashSetWrapper<SingleValue> source, ISymbol target, OperationBlockAnalysisContext context, Location location, bool targetIsMethodReturn)
		{
			if (source.Values == null)
				return;

			foreach (var sourceValue in source.Values)
				CheckAndReportDynamicallyAccessedMembers (sourceValue.Symbol.Symbol, target, context, location, targetIsMethodReturn);
		}

		static void CheckAndReportDynamicallyAccessedMembers (HashSetWrapper<SingleValue> source, HashSetWrapper<SingleValue> target, OperationBlockAnalysisContext context, Location location)
		{
			if (target.Values == null)
				return;

			foreach (var targetValue in target.Values)
				CheckAndReportDynamicallyAccessedMembers (source, targetValue.Symbol.Symbol, context, location, targetIsMethodReturn: targetValue.Symbol.MethodReturn);
		}

		static void CheckAndReportDynamicallyAccessedMembers (ISymbol source, ISymbol target, OperationBlockAnalysisContext context, Location location, bool targetIsMethodReturn)
		{
			// For the target symbol, a method symbol may represent either a "this" parameter or a method return.
			// The target symbol should never be a named type.
			Debug.Assert (target.Kind is not SymbolKind.NamedType);
			var damtOnTarget = targetIsMethodReturn
				? ((IMethodSymbol) target).GetDynamicallyAccessedMemberTypesOnReturnType ()
				: target.GetDynamicallyAccessedMemberTypes ();
			// For the source symbol, a named type represents a "this" parameter and a method symbol represents a method return.
			var damtOnSource = source.Kind switch {
				SymbolKind.NamedType => context.OwningSymbol.GetDynamicallyAccessedMemberTypes (),
				SymbolKind.Method => ((IMethodSymbol) source).GetDynamicallyAccessedMemberTypesOnReturnType (),
				_ => source.GetDynamicallyAccessedMemberTypes ()
			};

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				return;

			var diag = GetDiagnosticId (source.Kind, target.Kind, targetIsMethodReturn);
			var diagArgs = GetDiagnosticArguments (source.Kind == SymbolKind.NamedType ? context.OwningSymbol : source, target, missingAnnotations);

			context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (diag), location, diagArgs));
		}

		static DiagnosticId GetDiagnosticId (SymbolKind source, SymbolKind target, bool targetIsMethodReturnType = false)
			=> (source, target) switch {
				(SymbolKind.Parameter, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsField,
				(SymbolKind.Parameter, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsThisParameter,
				(SymbolKind.Parameter, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchParameterTargetsParameter,
				(SymbolKind.Field, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsParameter,
				(SymbolKind.Field, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsField,
				(SymbolKind.Field, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsThisParameter,
				(SymbolKind.Field, SymbolKind.TypeParameter) => DiagnosticId.DynamicallyAccessedMembersMismatchFieldTargetsGenericParameter,
				(SymbolKind.NamedType, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsThisParameter,
				(SymbolKind.Method, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsField,
				(SymbolKind.Method, SymbolKind.Method) => targetIsMethodReturnType ?
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsMethodReturnType :
					DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsThisParameter,
				// Source here will always be a method's return type.
				(SymbolKind.Method, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchMethodReturnTypeTargetsParameter,
				(SymbolKind.NamedType, SymbolKind.Field) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsField,
				(SymbolKind.NamedType, SymbolKind.Parameter) => DiagnosticId.DynamicallyAccessedMembersMismatchThisParameterTargetsParameter,
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
	}
}
