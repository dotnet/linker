// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.IO;
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
				context.RegisterAdditionalFileAction (additionalFileContext => {
					// Check if it's a LinkAttributes xml
					if (_linkAttributesRegex.IsMatch (Path.GetFileName (additionalFileContext.AdditionalFile.Path))) {
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
							HandleTypeNode (typeNode, additionalFileContext);
						}
						foreach (var assemblyNode in xmlData.Assemblies) {
							assemblyNode.ReportDiagnostics (additionalFileContext);
							foreach (var typeNode in assemblyNode.Types) {
								HandleTypeNode (typeNode, additionalFileContext);
							}
						}
					}
				});
			});
		}

		private void HandleTypeNode (LinkAttributes.TypeNodeBase node, AdditionalFileAnalysisContext context)
		{
			foreach (var methodNode in node.Methods) {
				methodNode.ReportDiagnostics (context);
				foreach (var parameterNode in methodNode.Parameters) {
					parameterNode.ReportDiagnostics (context);
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
				HandleTypeNode (nestedTypeNode, context);
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
		}
	}
}
