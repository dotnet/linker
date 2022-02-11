// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class XmlAnalyzer : DiagnosticAnalyzer
	{
		// Returns the injected attributes once the xml has been resolved
		internal static async Task<Dictionary<ISymbol, ImmutableArray<IAttributeData>>?> GetInjectedAttributesAsync ()
		{
			await InjectedAttributesSemaphore.WaitAsync ();
			InjectedAttributesSemaphore.Release ();
			return InjectedAttributes;
		}

		private static Dictionary<ISymbol, ImmutableArray<IAttributeData>>? InjectedAttributes;

		static readonly SemaphoreSlim InjectedAttributesSemaphore = new SemaphoreSlim (0);

		private static readonly DiagnosticDescriptor s_errorProcessingXmlLocation = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation);

		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (
			s_errorProcessingXmlLocation,
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.XmlMoreThanOneValueForParameterOfMethod),
			DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.XmlMoreThanOneReturnElementForMethod));

		private static readonly Regex _linkAttributesRegex = new (@"ILLink\.LinkAttributes.*\.xml");

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				// Reolve types and build attributes as soon as possible to be used in other analyzers
				foreach (var additionalFile in context.Options.AdditionalFiles) {
					// Check if it's a LinkAttributes xml
					if (!_linkAttributesRegex.IsMatch (Path.GetFileName (additionalFile.Path)))
						return;
					if (additionalFile.GetText () is not SourceText text)
						return;

					if (!context.TryGetValue (text, ProcessXmlProvider, out var xmlData) || xmlData is null) {
						return;
					}

					foreach (var typeNode in xmlData.Types) {
						IEnumerable<(INamedTypeSymbol, IEnumerable<IAttributeData>)> injections = ResolveType (typeNode, context.Compilation);

					}
					// Resolve types immediately, put additional diagnostics in the model to be reported in 
					//	the AdditionalFileContext.
				}

				// Report Diagnostics in tree
				context.RegisterAdditionalFileAction (additionalFileContext => {
					if (!_linkAttributesRegex.IsMatch (Path.GetFileName (additionalFileContext.AdditionalFile.Path)))
						return;
					if (additionalFileContext.AdditionalFile.GetText () is not SourceText text)
						return;

					if (!context.TryGetValue (text, ProcessXmlProvider, out var xmlData) || xmlData is null) {
						additionalFileContext.ReportDiagnostic (Diagnostic.Create (
							s_errorProcessingXmlLocation,
							null,
							additionalFileContext.AdditionalFile.Path));
						return;
					}

					foreach (var typeNode in xmlData.Types) {
						ReportTypeNodeDiagnostics (typeNode, additionalFileContext);
					}
					foreach (var assemblyNode in xmlData.Assemblies) {
						assemblyNode.ReportDiagnostics (additionalFileContext);
						foreach (var typeNode in assemblyNode.Types) {
							ReportTypeNodeDiagnostics (typeNode, additionalFileContext);
						}
					}
				});
			});
		}

		private void ReportTypeNodeDiagnostics (LinkAttributes.TypeNodeBase node, AdditionalFileAnalysisContext context)
		{
			foreach (var methodNode in node.Methods) {
				methodNode.ReportDiagnostics (context);
				foreach (var parameterNode in methodNode.Parameters) {
					parameterNode.ReportDiagnostics (context);
				}
				foreach (var attributeNode in methodNode.ReturnAttributes) {
					attributeNode.ReportAttributeDiagnostics (context);
				}
			}
			foreach (var eventNode in node.Events) {
				eventNode.ReportDiagnostics (context);
			}
			foreach (var field in node.Fields) {
				field.ReportDiagnostics (context);
			}
			foreach (var propertyNode in node.Properties) {
				propertyNode.ReportDiagnostics (context);
			}
			foreach (var nestedTypeNode in node.Types) {
				ReportTypeNodeDiagnostics (nestedTypeNode, context);
			}
		}

		// Resolves a top level type and all the containing nodes
		private IEnumerable<(INamedTypeSymbol, IEnumerable<IAttributeData>)>? ResolveType (
			LinkAttributes.TypeNode typeNode,
			Compilation compilation)
		{
			var types = compilation.GetSymbolsWithName (typeNode.FullName);
			if (types.Count() != 1) {
				if (typeNode.Diagnostics is null)
					typeNode.Diagnostics = new List<LinkAttributes.XmlDiagnostic> ();
				typeNode.Diagnostics.Add (
					new LinkAttributes.XmlDiagnostic (DiagnosticId.XmlCouldNotResolveType, typeNode.FullName));
				return null;
			}
			var resolvedType = types.First ()!;
			return ResolveTypeMembers (typeNode, resolvedType);
		}

		private IEnumerable<(INamedTypeSymbol, IEnumerable<IAttributeData>)>? ResolveTypeMembers (
			LinkAttributes.TypeNodeBase typeNode,
			INamedTypeSymbol resolvedType)
		{
			var injections = Array.Empty<(INamedTypeSymbol, IEnumerable<IAttributeData>)> ().AsEnumerable ();

			foreach (var methodNode in typeNode.Methods) {

			}
			foreach (var eventNode in typeNode.Events) {
			}
			foreach (var fieldNode in typeNode.Fields) {

			}
			foreach (var propertyNode in typeNode.Properties) {
			}
			foreach (var nestedTypeNode in typeNode.Types) {
				var matches = resolvedType.GetTypeMembers ().Where (type => type.Name == nestedTypeNode.Name);
				if (matches.Count() != 1) {
					nestedTypeNode.AddDiagnostic(DiagnosticId.XmlCouldNotResolveType, nestedTypeNode.Name);
					continue;
				}
				var resolvedNestedType = matches.First ();
				injections = injections.Concat (ResolveTypeMembers (nestedTypeNode, resolvedNestedType));
			}
		}

		private static Stream GenerateStream (SourceText xmlText)
		{
			MemoryStream stream = new MemoryStream ();
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				xmlText.Write (writer);
			}
			stream.Position = 0;
			return stream;
		}

		// Used in context.TryGetValue to cache the xml model
		public static readonly SourceTextValueProvider<LinkAttributes.LinkerNode?> ProcessXmlProvider = new ((sourceText) => {
			Stream stream = GenerateStream (sourceText);
			XDocument? document;
			try {
				document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			} catch (XmlException) {
				return null;
			}
			return LinkAttributes.ProcessXml (document);
		});
	}
	static class IXmlLineInfoExtensions
	{
		public static Location ToLocation (this IXmlLineInfo xmlLineInfo, string filename)
		{
			var linePosition = new LinePosition (xmlLineInfo.LineNumber, xmlLineInfo.LinePosition);
			return Location.Create (filename, new TextSpan (), new LinePositionSpan (linePosition, linePosition));
		}
	}

	static class NodeBaseExtensions
	{
		// Reports all diagnostics from the node itself and any of the attributes in it, but no more
		public static void ReportDiagnostics (this LinkAttributes.NodeBase node, AdditionalFileAnalysisContext context)
		{
			if (node.Diagnostics is null)
				return;
			foreach (var diagnostic in node.Diagnostics) {
				context.ReportDiagnostic (Diagnostic.Create (
					DiagnosticDescriptors.GetDiagnosticDescriptor (diagnostic.DiagnosticId),
					node.LineInfo?.ToLocation (context.AdditionalFile.Path),
					diagnostic.MessageArgs));
			}
			if (node is LinkAttributes.AttributeTargetNode attributeProviderNode) {
				foreach (var attributeNode in attributeProviderNode.Attributes) {
					attributeNode.ReportAttributeDiagnostics (context);
				}
			}
		}
		public static void ReportAttributeDiagnostics(this LinkAttributes.AttributeNode node, AdditionalFileAnalysisContext context)
		{
			node.ReportDiagnostics (context);
			foreach (var attributeArgumentNode in node.Arguments) {
				attributeArgumentNode.ReportDiagnostics (context);
				var argNode = attributeArgumentNode;
				while (argNode is LinkAttributes.AttributeArgumentBoxNode boxNode) {
					argNode = boxNode.InnerArgument;
					argNode?.ReportDiagnostics (context);
				}
			}
		}
		public static void AddDiagnostic(this LinkAttributes.NodeBase node, DiagnosticId diagnosticId, params string[] args)
		{
			if (node.Diagnostics is null)
				node.Diagnostics = new List<LinkAttributes.XmlDiagnostic> ();
			node.Diagnostics.Add(new LinkAttributes.XmlDiagnostic (diagnosticId, args));
		}
	}
}
