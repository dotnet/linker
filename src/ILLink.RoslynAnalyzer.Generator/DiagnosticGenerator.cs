// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ILLink.RoslynAnalyzer.Generator
{
	[Generator]
	public class DiagnosticGenerator : ISourceGenerator
	{
		public void Execute (GeneratorExecutionContext context)
		{
			StringBuilder sb = new StringBuilder ();
			sb.Append (@"
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using ILLink.Shared;

namespace ILLink.RoslynAnalyzer
{
	public static partial class Diagnostics
	{
		static partial void AddGeneratedDiagnostics ()
		{
");

			// Add all types with supported diagnostics and the diagnostic codes that these support.
			SyntaxContextReceiver scr = (SyntaxContextReceiver) context.SyntaxContextReceiver!;
			HashSet<Diagnostic> diagnostics = new HashSet<Diagnostic> ();

			foreach (var typeNameWithAddedDiagnostics in scr.GetTypeNamesWithSupportedDiagnostics ()) {
				sb.Append (@$"
			_supportedDiagnosticsOnType[typeof ({typeNameWithAddedDiagnostics})] = new List<string> {{");
				foreach (var supportedDiagnosticOnType in scr.GetSupportedDiagnosticsOnType (typeNameWithAddedDiagnostics)) {
					var diagnosticCode = supportedDiagnosticOnType.Code;
					diagnostics.Add (supportedDiagnosticOnType);
					sb.Append (@$"
				""{diagnosticCode}"",");
				}

				sb.Append (@"
			};
");
			}

			// For each diagnositc, add its correspondent diagnostic descriptor.
			foreach (var diagnostic in diagnostics) {
				sb.Append ($@"
			_generatedDiagnostics[""{diagnostic.Code}""] = new DiagnosticDescriptor (
				""{diagnostic.Code}"",
				new LocalizableResourceString (nameof (SharedStrings.{
					(!string.IsNullOrEmpty (diagnostic.UseExistingMessageTitleResourceString) ?
						diagnostic.UseExistingMessageTitleResourceString : diagnostic.Name + "Title")}),
					SharedStrings.ResourceManager, typeof (SharedStrings)),
				new LocalizableResourceString (nameof (SharedStrings.{
					(!string.IsNullOrEmpty (diagnostic.UseExistingMessageResourceString) ?
						diagnostic.UseExistingMessageResourceString : diagnostic.Name + "Message")}),
					SharedStrings.ResourceManager, typeof (SharedStrings)),
				""{(!string.IsNullOrEmpty (diagnostic.Category) ? diagnostic.Category : "Trimming")}"",
				DiagnosticSeverity.Warning,
				isEnabledByDefault: {(string.IsNullOrEmpty (diagnostic.IsEnabledByDefault) ? "true" : diagnostic.IsEnabledByDefault)}");

				if (!string.IsNullOrEmpty (diagnostic.HelpLinkURI))
					sb.Append ($@",
				helpLinkUri: ""{diagnostic.HelpLinkURI}"""
);

				sb.Append (@");");
			}

			sb.Append (@"
		}
	}
}");

			context.AddSource ("Diagnostics.Generated", SourceText.From (sb.ToString (), Encoding.UTF8));
		}

		public void Initialize (GeneratorInitializationContext context)
		{
			context.RegisterForSyntaxNotifications (() => new SyntaxContextReceiver ());
		}

		class SyntaxContextReceiver : ISyntaxContextReceiver
		{
			readonly HashSet<string> _typeNamesWithAddedDiagnostics = new HashSet<string> ();
			readonly Dictionary<string, List<Diagnostic>> _supportedDiagnostics = new Dictionary<string, List<Diagnostic>> ();

			public ImmutableArray<string> GetTypeNamesWithSupportedDiagnostics () => ImmutableArray.Create (_typeNamesWithAddedDiagnostics.ToArray ());

			public ImmutableArray<Diagnostic> GetSupportedDiagnosticsOnType (string typeName)
			{
				if (!_supportedDiagnostics.TryGetValue (typeName, out var _diagnostics))
					return ImmutableArray<Diagnostic>.Empty;

				return ImmutableArray.Create (_diagnostics.ToArray ());
			}

			public void OnVisitSyntaxNode (GeneratorSyntaxContext context)
			{
				if (context.Node is AttributeSyntax attribute && attribute.Name.ToString () == "AddSupportedDiagnostic") {
					var typeDeclaration = attribute.Ancestors ().OfType<ClassDeclarationSyntax> ().First ();
					var typeSymbol = context.SemanticModel.GetDeclaredSymbol (typeDeclaration) as INamedTypeSymbol;
					bool isDiagnosticAnalyzer = false;
					while (typeSymbol != null) {
						if (typeSymbol.Name == nameof (DiagnosticAnalyzer)) {
							isDiagnosticAnalyzer = true;
							break;
						}

						typeSymbol = typeSymbol.BaseType;
					}

					if (!isDiagnosticAnalyzer)
						return;

					var typeName = typeDeclaration.Identifier.ToString ();
					_typeNamesWithAddedDiagnostics.Add (typeName);
					if (!_supportedDiagnostics.TryGetValue (typeName, out var supportedDiagnostics)) {
						supportedDiagnostics = new List<Diagnostic> ();
						_supportedDiagnostics.Add (typeName, supportedDiagnostics);
					}

					var arguments = attribute.ArgumentList!.Arguments;
					Diagnostic diagnostic = new Diagnostic ();

					diagnostic.Code = context.SemanticModel.GetConstantValue (arguments[0].Expression).ToString ();
					diagnostic.Name = context.SemanticModel.GetConstantValue (arguments[1].Expression).ToString ();

					// Optional arguments
					for (int i = 2; i < arguments.Count; i++) {
						var argName = arguments[i].NameEquals!.Name.ToString ();
						var argValue = context.SemanticModel.GetConstantValue (arguments[i].Expression).ToString ();
						switch (argName) {
						case "Category":
							diagnostic.Category = argValue;
							break;

						case "IsEnabledByDefault":
							diagnostic.IsEnabledByDefault = argValue;
							break;

						case "HelpLinkURI":
							diagnostic.HelpLinkURI = argValue;
							break;

						case "UseExistingMessageTitleResourceString":
							diagnostic.UseExistingMessageTitleResourceString = argValue;
							break;

						case "UseExistingMessageResourceString":
							diagnostic.UseExistingMessageResourceString = argValue;
							break;

						default:
							throw new ArgumentException ($"An unsupported property '{argName}' was used on {attribute.FullSpan}.");
						}
					}

					supportedDiagnostics.Add (diagnostic);
				}
			}
		}
	}
}
