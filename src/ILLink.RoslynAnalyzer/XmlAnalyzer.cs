// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Data;
using System.Xml.Schema;
using System.Xml;

namespace ILLink.RoslynAnalyzer
{
	public abstract class XmlAnalyzer : DiagnosticAnalyzer
	{
		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationAction (context => {
				if (context.GetLinkAttributesSourceText () is not SourceText text)
					return;
				var stream = GenerateStream (text);
				XDocument document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
				XmlSchemaSet schemaSet = new XmlSchemaSet ();
				var schemaStream = XmlReader.Create ("../../ILLink.Shared/ILLink.LinkAttributes.xsd");
				var schema = XmlSchema.Read (schemaStream, null);
				schemaSet.Add (schema);
				document.Validate (schemaSet, (sender, error) => {
					context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation), null, error.Message));
				});
			});
			context.RegisterCompilationStartAction (context => {
				if (context.GetLinkAttributesSourceText () is not SourceText text)
					return;
				if (!context.TryGetValue (text, ProcessXmlProvider, out var xmldata))
					return;
				foreach (var root in xmldata) {
					if (root is LinkAttributes.TypeNode typeNode) {
						if (typeNode.Attributes.Count > 0) {

						}
						context.RegisterSymbolAction (context => {
							// Do things
						}, SymbolKind.NamedType);
					}
				}
			});
		}
		
		public Dictionary<LinkAttributes.AttributeTargetNode, ISymbol> NodeMapping = new ();

		public Dictionary<LinkAttributes.AttributeNode, AttributeData> AttributeMap = new ();

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
		public static readonly SourceTextValueProvider<List<LinkAttributes.IRootNode>> ProcessXmlProvider = new SourceTextValueProvider<List<LinkAttributes.IRootNode>> ((sourceText) => {
			Stream stream = GenerateStream (sourceText);
			XDocument document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			return LinkAttributes.ProcessXml (document);
			});
	}

	public static class ContextExtensions
	{
		public static SourceText? GetLinkAttributesSourceText (this CompilationStartAnalysisContext context)
		{
			return context.Options.AdditionalFiles.FirstOrDefault (file => Path.GetFileName (file.Path).Contains ("ILLink.LinkAttributes.xml"))?.GetText (context.CancellationToken);
		}
		
		public static SourceText? GetLinkAttributesSourceText (this CompilationAnalysisContext context)
		{
			return context.Options.AdditionalFiles.FirstOrDefault (file => Path.GetFileName (file.Path).Contains ("ILLink.LinkAttributes.xml"))?.GetText (context.CancellationToken);
		}
	}
}
