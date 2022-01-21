// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Reflection.Runtime.TypeParsing;

namespace ILLink.RoslynAnalyzer
{
	public abstract class XmlAnalyzer : DiagnosticAnalyzer
	{
		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				Dictionary<InjectedType, List<InjectedAttribute>> xmldata = AnalyzerXmlAttributeParser.ProcessXml (context);

				foreach (var pair in xmldata) {
					var type = pair.Key;
					var attribute = pair.Value;
					SymbolKind? symbolKind = type.AttributeTarget switch {
						AttributeTargets.Field => SymbolKind.Field,
						AttributeTargets.Property => SymbolKind.Property,
						AttributeTargets.Event => SymbolKind.Event,
						AttributeTargets.Method => SymbolKind.Method,
						AttributeTargets.Class => SymbolKind.NamedType,
						AttributeTargets.Assembly => SymbolKind.Assembly,
						AttributeTargets.Parameter => SymbolKind.Parameter,
						//case AttributeTargets.ReturnValue _ => SymbolKind.Return
						_ => null
					};
					if (symbolKind is null) {
						Debug.Fail ($"Unknown Attribute target: {nameof (type.AttributeTarget)}");
						continue;
					}
					
					context.RegisterSymbolAction (symbolContext => {
						TypeName parsedTypeName;
						var symbol = symbolContext.Symbol;
						try {
							parsedTypeName = TypeParser.ParseTypeName (type.Fullname);
						} catch (ArgumentException) {
							symbolContext.ReportDiagnostic(DiagnosticId.xml)
						} catch (System.IO.FileLoadException) {
							return false;
						}
					}, (SymbolKind)symbolKind);
					
				}
			});
		}
	}

	partial class InjectedType
	{
		public SymbolKind? Kind {
			get {
				return this.AttributeTarget switch {
					AttributeTargets.Field => SymbolKind.Field,
					AttributeTargets.Property => SymbolKind.Property,
					AttributeTargets.Event => SymbolKind.Event,
					AttributeTargets.Method => SymbolKind.Method,
					AttributeTargets.Class => SymbolKind.NamedType,
					AttributeTargets.Assembly => SymbolKind.Assembly,
					AttributeTargets.Parameter => SymbolKind.Parameter,
					//case AttributeTargets.ReturnValue _ => SymbolKind.Return
					_ => null
				};
			}
		}
		public bool RefersTo (ISymbol symbol)
		{
			if (symbol is INamedTypeSymbol namedTypeSymbol) {
				namedTypeSymbol.
			}
			if (symbol is IAssemblySymbol assemblySymbol) {
				assemblySymbol.
		}
		
	}
}
