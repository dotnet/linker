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

namespace ILLink.RoslynAnalyzer
{
	public abstract class XmlAnalyzer : DiagnosticAnalyzer
	{
		public override void Initialize (AnalysisContext context)
		{
			context.EnableConcurrentExecution ();
			context.ConfigureGeneratedCodeAnalysis (GeneratedCodeAnalysisFlags.ReportDiagnostics);
			context.RegisterCompilationStartAction (context => {
				var documentStream = GenerateStream ("", context);
				var document = XDocument.Load (documentStream, LoadOptions.SetLineInfo);
				// Check against Schema
				var xmldata = LinkAttributes.ProcessXml (document);

			});
		}
		
		private static Stream? GenerateStream (string xmlDocumentLocation, CompilationStartAnalysisContext context)
		{
			ImmutableArray<AdditionalText> additionalFiles = context.Options.AdditionalFiles;
			AdditionalText? xmlFile = additionalFiles.FirstOrDefault (file => Path.GetFileName (file.Path).Contains (xmlDocumentLocation));
			if (xmlFile == null) {
				return null;
			}
			SourceText? fileText = xmlFile.GetText (context.CancellationToken);
			if (fileText == null) {
				throw new NotImplementedException ();
			}
			MemoryStream stream = new MemoryStream ();
			using (StreamWriter writer = new StreamWriter (stream, Encoding.UTF8, 1024, true)) {
				fileText.Write (writer);
			}

			stream.Position = 0;
			return stream;
		}

	}
}
