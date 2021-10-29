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

		static void CheckAndReportDynamicallyAccessedMembers (HashSetWrapper<SingleValue> source, HashSetWrapper<SingleValue> target, OperationBlockAnalysisContext context, Location location)
		{
			if (target.Values == null)
				return;

			foreach (var targetValue in target.Values)
				CheckAndReportDynamicallyAccessedMembers (source, targetValue, context, location);
		}

		static void CheckAndReportDynamicallyAccessedMembers (HashSetWrapper<SingleValue> source, SingleValue target, OperationBlockAnalysisContext context, Location location)
		{
			if (source.Values == null)
				return;

			foreach (var sourceValue in source.Values)
				CheckAndReportDynamicallyAccessedMembers (sourceValue, target, context, location);
		}

		static void CheckAndReportDynamicallyAccessedMembers (SingleValue sourceValue, SingleValue targetValue, OperationBlockAnalysisContext context, Location location)
		{
			if (sourceValue is not AnnotatedSymbol source || targetValue is not AnnotatedSymbol target)
				return;

			Debug.Assert (target.Source.Kind is not SymbolKind.NamedType);
			var damtOnTarget = target.DynamicallyAccessedMemberTypes;
			var damtOnSource = source.DynamicallyAccessedMemberTypes;

			if (Annotations.SourceHasRequiredAnnotations (damtOnSource, damtOnTarget, out var missingAnnotations))
				return;

			var diag = GetDiagnosticId (source.Source.Kind, target.Source.Kind, target.IsMethodReturn);
			var diagArgs = GetDiagnosticArguments (source.Source.Kind == SymbolKind.NamedType ? context.OwningSymbol : source.Source, target.Source, missingAnnotations);

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
