// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	internal class TestChecker
	{
		private readonly CompilationWithAnalyzers Compilation;

		private readonly SemanticModel SemanticModel;

		private readonly List<(string Id, string Message)> DiagnosticMessages;

		public TestChecker (SyntaxNode syntaxNode, (CompilationWithAnalyzers Compilation, SemanticModel SemanticModel) compilationResult)
		{
			Compilation = compilationResult.Compilation;
			SemanticModel = compilationResult.SemanticModel;
			DiagnosticMessages = Compilation.GetAnalyzerDiagnosticsAsync ().Result
				.Where (d => d.Location.SourceSpan.IntersectsWith (syntaxNode.Span))
				.Select (d => (d.Id, d.GetMessage ()))
				.ToList ();
		}

		bool IsExpectedDiagnostic (AttributeSyntax attribute) {
			switch (attribute.Name.ToString()) {
			case "ExpectedWarning":
			case "LogContains":
			case "UnrecognizedReflectionAccessPattern":
				return true;
			default:
				return false;
			}
		}

		int? ValidateExpectedDiagnostic (AttributeSyntax attribute, List<(string Id, string Message)> diagnosticMessages, out string? missingDiagnosticMessage)
		{
			switch (attribute.Name.ToString ()) {
			case "ExpectedWarning":
				return ValidateExpectedWarningAttribute (attribute!, diagnosticMessages, out missingDiagnosticMessage);
			case "LogContains":
				return ValidateLogContainsAttribute (attribute!, diagnosticMessages, out missingDiagnosticMessage);
			case "UnrecognizedReflectionAccessPattern":
				return ValidateUnrecognizedReflectionAccessPatternAttribute (attribute!, diagnosticMessages, out missingDiagnosticMessage);
			}
			missingDiagnosticMessage = null;
			return null;
		}

		public void ValidateAttributes (List<AttributeSyntax> attributes)
		{
			var unmatchedDiagnostics = DiagnosticMessages;

			var missingDiagnostics = new List<(AttributeSyntax Attribute, string Message)> ();
			foreach (var attribute in attributes) {
				if (attribute.Name.ToString() == "LogDoesNotContain")
					ValidateLogDoesNotContainAttribute (attribute, DiagnosticMessages);
				
				if (!IsExpectedDiagnostic (attribute))
					continue;

				var matchIndex = ValidateExpectedDiagnostic (attribute, unmatchedDiagnostics, out string missingDiagnosticMessage);
				if (matchIndex == null) {
					missingDiagnostics.Add ((attribute, missingDiagnosticMessage));
					continue;
				}

				unmatchedDiagnostics.RemoveAt (matchIndex.Value);
			}

			var missingDiagnosticsMessage = missingDiagnostics.Any ()
				? $"Missing diagnostics:{Environment.NewLine}{string.Join (Environment.NewLine, missingDiagnostics.Select (md => md.Message))}"
				: String.Empty;

			var unmatchedDiagnosticsMessage = unmatchedDiagnostics.Any ()
				? $"Found unmatched diagnostics:{Environment.NewLine}{string.Join (Environment.NewLine, unmatchedDiagnostics)}"
				: String.Empty;

			Assert.True (!missingDiagnostics.Any (), $"{missingDiagnosticsMessage}{Environment.NewLine}{unmatchedDiagnosticsMessage}");
			Assert.True (!unmatchedDiagnostics.Any (), unmatchedDiagnosticsMessage);
		}

		private int? ValidateExpectedWarningAttribute (AttributeSyntax attribute, List<(string Id, string Message)> diagnosticMessages, out string missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			var args = TestCaseUtils.GetAttributeArguments (attribute);
			string expectedWarningCode = TestCaseUtils.GetStringFromExpression (args["#0"]);

			if (!expectedWarningCode.StartsWith ("IL"))
				return null;

			if (args.TryGetValue ("ProducedBy", out var producedBy) &&
				producedBy is MemberAccessExpressionSyntax memberAccessExpression &&
				memberAccessExpression.Name is IdentifierNameSyntax identifierNameSyntax &&
				identifierNameSyntax.Identifier.ValueText == "Trimmer")
				return;

			List<string> expectedMessages = args
				.Where (arg => arg.Key.StartsWith ("#") && arg.Key != "#0")
				.Select (arg => TestCaseUtils.GetStringFromExpression (arg.Value, SemanticModel))
				.ToList ();

			for (int i = 0; i < diagnosticMessages.Count; i++) {
				if (Matches (diagnosticMessages[i]))
					return i;
			}

			missingDiagnosticMessage = $"Expected to find warning containing:{string.Join (" ", expectedMessages.Select (m => "'" + m + "'"))}" +
					$", but no such message was found.{ Environment.NewLine}";
			return null;

			bool Matches ((string Id, string Message) mc) {
				if (mc.Id != expectedWarningCode)
					return false;

				foreach (var expectedMessage in expectedMessages)
					if (!mc.Message.Contains (expectedMessage))
						return false;

				return true;
			}
		}

		private int? ValidateLogContainsAttribute (AttributeSyntax attribute, List<(string Id, string Message)> diagnosticMessages, out string missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			var arg = Assert.Single (TestCaseUtils.GetAttributeArguments (attribute));
			var text = TestCaseUtils.GetStringFromExpression (arg.Value);

			// If the text starts with `warning IL...` then it probably follows the pattern
			//	'warning <diagId>: <location>:'
			// We don't want to repeat the location in the error message for the analyzer, so
			// it's better to just trim here. We've already filtered by diagnostic location so
			// the text location shouldn't matter
			if (text.StartsWith ("warning IL")) {
				var firstColon = text.IndexOf (": ");
				if (firstColon > 0) {
					var secondColon = text.IndexOf (": ", firstColon + 1);
					if (secondColon > 0) {
						text = text.Substring (secondColon + 2);
					}
				}
			}

			for (int i = 0; i < diagnosticMessages.Count; i++) {
				if (diagnosticMessages[i].Message.Contains (text))
					return i;
			}

			missingDiagnosticMessage = $"Could not find text:\n{text}\nIn diagnostics:\n{(string.Join (Environment.NewLine, DiagnosticMessages))}";
			return null;
		}

		private void ValidateLogDoesNotContainAttribute (AttributeSyntax attribute, List<(string Id, string Message)> diagnosticMessages)
		{
			var arg = Assert.Single (TestCaseUtils.GetAttributeArguments (attribute));
			var text = TestCaseUtils.GetStringFromExpression (arg.Value);
			foreach (var diagnostic in DiagnosticMessages)
				Assert.DoesNotContain (text, diagnostic.Message);
		}

		private int? ValidateUnrecognizedReflectionAccessPatternAttribute (AttributeSyntax attribute, List<(string Id, string Message)> diagnosticMessages, out string missingDiagnosticMessage)
		{
			missingDiagnosticMessage = null;
			var args = TestCaseUtils.GetAttributeArguments (attribute);

			MemberDeclarationSyntax sourceMember = attribute.Ancestors ().OfType<MemberDeclarationSyntax> ().First ();
			if (SemanticModel.GetDeclaredSymbol (sourceMember) is not ISymbol memberSymbol)
				return null;

			string sourceMemberName = memberSymbol!.GetDisplayName ();
			string expectedReflectionMemberMethodType = TestCaseUtils.GetStringFromExpression (args["#0"], SemanticModel);
			string expectedReflectionMemberMethodName = TestCaseUtils.GetStringFromExpression (args["#1"], SemanticModel);

			var reflectionMethodParameters = new List<string> ();
			if (args.TryGetValue("#2", out var reflectionMethodParametersExpr) || args.TryGetValue("reflectionMethodParameters", out reflectionMethodParametersExpr)) {
				if (reflectionMethodParametersExpr is ArrayCreationExpressionSyntax arrayReflectionMethodParametersExpr) {
					foreach (var rmp in arrayReflectionMethodParametersExpr.Initializer!.Expressions)
						reflectionMethodParameters.Add (TestCaseUtils.GetStringFromExpression (rmp, SemanticModel));
				}
			}

			var expectedStringsInMessage = new List<string> ();
			if (args.TryGetValue("#3", out var messageExpr) || args.TryGetValue("message", out messageExpr)) {
				if (messageExpr is ArrayCreationExpressionSyntax arrayMessageExpr) {
					foreach (var m in arrayMessageExpr.Initializer!.Expressions)
						expectedStringsInMessage.Add (TestCaseUtils.GetStringFromExpression (m, SemanticModel));
				}
			}

			string expectedWarningCode = string.Empty;
			if (args.TryGetValue ("#4", out var messageCodeExpr) || args.TryGetValue ("messageCode", out messageCodeExpr)) {
				expectedWarningCode = TestCaseUtils.GetStringFromExpression (messageCodeExpr);
				Assert.True (expectedWarningCode.StartsWith ("IL"),
					$"The warning code specified in {messageCodeExpr.ToString ()} must start with the 'IL' prefix. Specified value: '{expectedWarningCode}'");
			}

			// Don't validate the return type becasue this is not included in the diagnostic messages.


			var sb = new StringBuilder ();

			// Format the member signature the same way Roslyn would since this is what will be included in the warning message.
			sb.Append (expectedReflectionMemberMethodType).Append (".").Append (expectedReflectionMemberMethodName);
			if (!expectedReflectionMemberMethodName.EndsWith (".get") &&
				!expectedReflectionMemberMethodName.EndsWith (".set") &&
				reflectionMethodParameters is not null)
				sb.Append ("(").Append (string.Join (", ", reflectionMethodParameters)).Append (")");

			var reflectionAccessPattern = sb.ToString ();

			for (int i = 0; i < diagnosticMessages.Count; i++) {
				if (Matches (diagnosticMessages[i]))
					return i;
			}

			missingDiagnosticMessage = $"Expected to find unrecognized reflection access pattern '{(expectedWarningCode == string.Empty ? "" : expectedWarningCode + " ")}" +
					$"{sourceMemberName}: Usage of {reflectionAccessPattern} unrecognized.";
			return null;

			bool Matches ((string Id, string Message) mc) {
				if (!string.IsNullOrEmpty (expectedWarningCode) && mc.Id != expectedWarningCode)
					return false;

				// Don't check whether the message contains the source member name. Roslyn's diagnostics don't include the source
				// member as part of the message.

				foreach (var expectedString in expectedStringsInMessage)
					if (!mc.Message.Contains (expectedString))
						return false;

				return mc.Message.Contains (reflectionAccessPattern);
			}
		}
	}
}
