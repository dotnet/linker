// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using ILLink.Shared;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer
{
	[DiagnosticAnalyzer (LanguageNames.CSharp)]
	public class XmlAnalyzer : DiagnosticAnalyzer
	{
		private static readonly DiagnosticDescriptor s_moreThanOneValueForParameterOfMethod = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.XmlMoreThanOneValyForParameterOfMethod);
		private static readonly DiagnosticDescriptor s_errorProcessingXmlLocation = DiagnosticDescriptors.GetDiagnosticDescriptor (DiagnosticId.ErrorProcessingXmlLocation);
		public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create (s_moreThanOneValueForParameterOfMethod, s_errorProcessingXmlLocation);

		private static readonly Regex _linkAttributesRegex = new (@"ILLink\.LinkAttributes.*\.xml");

		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);

			context.RegisterCompilationStartAction (context => {
				// Report Diagnostics on malformed XML with additionalFileContext and register actions to resolve names with (CompilationStartAnalysisContext) context.
				context.RegisterAdditionalFileAction (additionalFileContext => {
					// Check if it's a LinkAttributes xml
					if (_linkAttributesRegex.IsMatch (Path.GetFileName (additionalFileContext.AdditionalFile.Path))) {
						if (additionalFileContext.AdditionalFile.GetText () is not SourceText text)
							return;

						if (!ValidateLinkAttributesXml (additionalFileContext, text))
							return;

						if (!context.TryGetValue (text, ProcessXmlProvider, out var xmlData) || xmlData is null)
							return;
						foreach (var root in xmlData) {
							if (root is LinkAttributes.TypeNode typeNode) {
								foreach (var duplicatedMethods in typeNode.Methods.GroupBy (m => m.Name).Where (m => m.Count () > 0)) {
									additionalFileContext.ReportDiagnostic (Diagnostic.Create (s_moreThanOneValueForParameterOfMethod, null, duplicatedMethods.FirstOrDefault ().Name, typeNode.FullName));
								}
							}
						}
					}
				});
			});
		}

		private static XmlSchema GenerateLinkAttributesSchema ()
		{
			var assembly = Assembly.GetExecutingAssembly ();
			using var schemaStream = assembly.GetManifestResourceStream ("ILLink.RoslynAnalyzer.ILLink.LinkAttributes.xsd");
			using var reader = XmlReader.Create (schemaStream);
			var schema = XmlSchema.Read (
				reader,
				null);
			return schema;
		}
		private static readonly XmlSchema LinkAttributesSchema = GenerateLinkAttributesSchema ();

		private static bool ValidateLinkAttributesXml (AdditionalFileAnalysisContext context, SourceText text)
		{
			var xmlStream = GenerateStream (text);
			XDocument? document;
			try {
				document = XDocument.Load (xmlStream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			} catch (XmlException ex) {
				context.ReportDiagnostic (Diagnostic.Create (s_errorProcessingXmlLocation, null, ex.Message));
				return false;
			}
			XmlSchemaSet schemaSet = new XmlSchemaSet ();
			schemaSet.Add (LinkAttributesSchema);
			bool valid = true;
			document.Validate (schemaSet, (sender, error) => {
				context.ReportDiagnostic (Diagnostic.Create (s_errorProcessingXmlLocation, null, "ILLinkAttributes.xml': '" + "At line " + error.Exception.LineNumber + ": " + error.Message));
				valid = false;
			});
			return valid;
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
		public static readonly SourceTextValueProvider<List<LinkAttributes.IRootNode>?> ProcessXmlProvider = new ((sourceText) => {
			Stream stream = GenerateStream (sourceText);
			XDocument? document;
			try {
				document = XDocument.Load (stream, LoadOptions.SetLineInfo | LoadOptions.PreserveWhitespace);
			} catch (System.Xml.XmlException) {
				return null;
			}
			return LinkAttributes.ProcessXml (document);
		});
	}
}
