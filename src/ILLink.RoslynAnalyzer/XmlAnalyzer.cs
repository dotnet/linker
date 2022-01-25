// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Linq;
using ILLink.Shared;
using Microsoft.CodeAnalysis;using Microsoft.CodeAnalysis.Diagnostics;
using System.IO;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using System.Xml.Linq;
using System.Collections.Generic;
using System.Xml.Schema;
using System.Xml;
using System.Reflection;
using System.Text.RegularExpressions;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer(LanguageNames.CSharp)]
	public class XmlAnalyzer : DiagnosticAnalyzer
	{
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation));
		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationAction (context => {
				var assembly = Assembly.GetExecutingAssembly();
				using (var schemaStream = assembly.GetManifestResourceStream("ILLink.RoslynAnalyzer.ILLink.LinkAttributes.xsd"))
				using (var reader = XmlReader.Create(schemaStream))
				{
					XmlSchema schema = XmlSchema.Read(
						reader, 
						null);
					foreach (SourceText text in context.GetLinkAttributesSourceTexts ()) {
						var xmlStream = GenerateStream (text);
						XDocument? document;
						try {
							document = XDocument.Load (xmlStream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
						} catch (XmlException ex) {
							context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation), null, ex.Message));
							return;
						}
						XmlSchemaSet schemaSet = new XmlSchemaSet ();
						schemaSet.Add (schema);
						document.Validate (schemaSet, (sender, error) => {
							context.ReportDiagnostic (Diagnostic.Create (DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation), null, "ILLinkAttributes.xml': '" + "At line " + error.Exception.LineNumber + ": " + error.Message));
						});
					}
				}
			});

			context.RegisterCompilationStartAction (context => {
				foreach (SourceText text in context.GetLinkAttributesSourceTexts ()) {
					if (!context.TryGetValue (text, ProcessXmlProvider, out var xmlData) || xmlData is null)
						return;
					foreach (var root in xmlData) {
						if (root is LinkAttributes.TypeNode typeNode) {
							foreach (var attribute in typeNode.Attributes) {
								context.RegisterSymbolAction (context => {
									// Do things
								}, SymbolKind.NamedType);
							}
						}
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
		public static readonly SourceTextValueProvider<List<LinkAttributes.IRootNode>?> ProcessXmlProvider = new ((sourceText) => {
			Stream stream = GenerateStream (sourceText);
			XDocument? document;
			try {
				document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			}
			catch (System.Xml.XmlException) {
				return null;
			}
			return LinkAttributes.ProcessXml (document);
			});

	}

	public static class ContextExtensions
	{
		private static readonly Regex _regex = new (@"ILLink\.LinkAttributes.*\.xml");

		[System.Diagnostics.CodeAnalysis.SuppressMessage ("MicrosoftCodeAnalysisPerformance", "RS1012:Start action has no registered actions", Justification = "Part of an extension method")]
		public static IEnumerable<SourceText> GetLinkAttributesSourceTexts (this CompilationStartAnalysisContext context)
		{
			return context.Options.AdditionalFiles.Select (file => _regex.IsMatch(Path.GetFileName (file.Path)) ? file.GetText (context.CancellationToken) : null).Where((text) => text is not null)!;
		}
		
		public static IEnumerable<SourceText> GetLinkAttributesSourceTexts (this CompilationAnalysisContext context)
		{
			return context.Options.AdditionalFiles.Select (file => _regex.IsMatch(Path.GetFileName (file.Path)) ? file.GetText (context.CancellationToken) : null).Where((text) => text is not null)!;
		}
	}
}
