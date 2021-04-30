// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ILLink.CodeFix;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class TestCaseUtils
	{
		private const string requiresAssemblyFilesAttributeDefinition = @"
#nullable enable
namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Event | AttributeTargets.Method | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
	public sealed class RequiresAssemblyFilesAttribute : Attribute
	{
		public RequiresAssemblyFilesAttribute () { }
		public string? Message { get; set; }
		public string? Url { get; set; }
	}
}";
		private const string requiresUnreferencedCodeAttributeDefinition = @"
#nullable enable
namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.Constructor | AttributeTargets.Method, Inherited = false)]
	public sealed class RequiresUnreferencedCodeAttribute : Attribute
	{
		public RequiresUnreferencedCodeAttribute(string message) { Message = message; }
		public string Message { get; }
		public string? Url { get; set; }
	}
}";
		private const string unconditionalSuppressMessageAttributeDefinition = @"
#nullable enable
namespace System.Diagnostics.CodeAnalysis
{
	[AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class UnconditionalSuppressMessageAttribute : Attribute
    {
        public UnconditionalSuppressMessageAttribute (string category, string checkId)
		{
			Category = category;
			CheckId = checkId;
		}
		public string Category { get; }
		public string CheckId { get; }
		public string? Scope { get; set; }
		public string? Target { get; set; }
		public string? MessageId { get; set; }
		public string? Justification { get; set; }
	}
}";

		public static IEnumerable<object[]> GetTestData (string testSuiteName)
		{
			var testFile = File.ReadAllText (s_testFiles[testSuiteName][0]);

			var root = CSharpSyntaxTree.ParseText (testFile).GetRoot ();

			var attributes = root.DescendantNodes ()
				.OfType<AttributeSyntax> ()
				.Where (a => IsWellKnown (a));

			var methodsXattributes = root.DescendantNodes ()
				.OfType<MethodDeclarationSyntax> ()
				.Select (m => (m!, m.AttributeLists.SelectMany (
									 al => al.Attributes.Where (a => IsWellKnown (a)))
								  .ToList ()))
				.Where (mXattrs => mXattrs.Item2.Count > 0)
				.Distinct ()
				.ToList ();

			foreach (var (m, attrs) in methodsXattributes) {
				yield return new object[] { m, attrs };
			}

			static bool IsWellKnown (AttributeSyntax attr)
			{
				switch (attr.Name.ToString ()) {
				case "ExpectedWarning":
				case "LogContains":
				case "LogDoesNotContain":
					return true;
				}
				return false;
			}
		}

		internal static void RunTest (MethodDeclarationSyntax m, List<AttributeSyntax> attrs, params (string, string)[] MSBuildProperties)
		{
			var comp = CSharpAnalyzerVerifier<RequiresUnreferencedCodeAnalyzer>.CreateCompilation (m.SyntaxTree, MSBuildProperties).Result;
			var diags = comp.GetAnalyzerDiagnosticsAsync ().Result;

			var filtered = diags.Where (d => d.Location.SourceSpan.IntersectsWith (m.Span))
								.Select (d => d.GetMessage ());
			foreach (var attr in attrs) {
				switch (attr.Name.ToString ()) {
				case "ExpectedWarning":
					var expectedWarningCode = attr.ArgumentList!.Arguments[0];
					if (!GetStringFromExpr (expectedWarningCode.Expression).Contains ("IL"))
						break;
					List<string> expectedMessages = new List<string> ();
					foreach (var argument in attr.ArgumentList!.Arguments) {
						if (argument.NameEquals != null)
							Assert.True (false, $"Analyzer does not support named arguments at this moment: {argument.NameEquals} {argument.Expression}");
						expectedMessages.Add (GetStringFromExpr (argument.Expression));
					}
					expectedMessages.RemoveAt (0);
					Assert.True (
						filtered.Any (mc => {
							foreach (var expectedMessage in expectedMessages)
								if (!mc.Contains (expectedMessage))
									return false;
							return true;
						}),
					$"Expected to find warning containing:{string.Join (" ", expectedMessages.Select (m => "'" + m + "'"))}" +
					$", but no such message was found.{ Environment.NewLine}In diagnostics: {string.Join (Environment.NewLine, filtered)}");
					break;
				case "LogContains": {
						var arg = Assert.Single (attr.ArgumentList!.Arguments);
						var text = GetStringFromExpr (arg.Expression);
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
						bool found = false;
						foreach (var d in filtered) {
							if (d.Contains (text)) {
								found = true;
								break;
							}
						}
						if (!found) {
							var diagStrings = string.Join (Environment.NewLine, filtered);
							Assert.True (false, $@"Could not find text:
{text}
In diagnostics:
{diagStrings}");
						}
					}
					break;
				case "LogDoesNotContain": {
						var arg = Assert.Single (attr.ArgumentList!.Arguments);
						var text = GetStringFromExpr (arg.Expression);
						foreach (var d in filtered) {
							Assert.DoesNotContain (text, d);
						}
					}
					break;
				}
			}

			// Accepts string literal expressions or binary expressions concatenating strings
			static string GetStringFromExpr (ExpressionSyntax expr)
			{
				switch (expr.Kind ()) {
				case SyntaxKind.StringLiteralExpression:
					var strLiteral = (LiteralExpressionSyntax) expr;
					var token = strLiteral.Token;
					Assert.Equal (SyntaxKind.StringLiteralToken, token.Kind ());
					return token.ValueText;
				case SyntaxKind.AddExpression:
					var addExpr = (BinaryExpressionSyntax) expr;
					return GetStringFromExpr (addExpr.Left) + GetStringFromExpr (addExpr.Right);
				default:
					Assert.True (false, "Unsupported expr kind " + expr.Kind ());
					return null!;
				}
			}
		}

		private static readonly ImmutableDictionary<string, List<string>> s_testFiles = GetTestFilesByDirName ();

		private static ImmutableDictionary<string, List<string>> GetTestFilesByDirName ()
		{
			var builder = ImmutableDictionary.CreateBuilder<string, List<string>> ();

			foreach (var file in GetTestFiles ()) {
				var dirName = Path.GetFileName (Path.GetDirectoryName (file))!;
				if (builder.TryGetValue (dirName, out var sources)) {
					sources.Add (file);
				} else {
					sources = new List<string> () { file };
					builder[dirName] = sources;
				}
			}

			return builder.ToImmutable ();
		}

		private static IEnumerable<string> GetTestFiles ()
		{
			GetDirectoryPaths (out var rootSourceDir, out _);

			foreach (var subDir in Directory.EnumerateDirectories (rootSourceDir, "*", SearchOption.AllDirectories)) {
				var subDirName = Path.GetFileName (subDir);
				switch (subDirName) {
				case "bin":
				case "obj":
				case "Properties":
				case "Dependencies":
				case "Individual":
					continue;
				}

				foreach (var file in Directory.EnumerateFiles (subDir, "*.cs")) {
					yield return file;
				}
			}
		}

		internal static (string, string)[] UseMSBuildProperties (params string[] MSBuildProperties)
		{
			return MSBuildProperties.Select (msbp => ($"build_property.{msbp}", "true")).ToArray ();
		}

		internal static void GetDirectoryPaths (out string rootSourceDirectory, out string testAssemblyPath)
		{

#if DEBUG
			var configDirectoryName = "Debug";
#else
			var configDirectoryName = "Release";
#endif

#if NET6_0
			const string tfm = "net6.0";
#else
			const string tfm = "net5.0";
#endif

			// working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var artifactsBinDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..");
			rootSourceDirectory = Path.GetFullPath (Path.Combine (artifactsBinDir, "..", "..", "test", "Mono.Linker.Tests.Cases"));
			testAssemblyPath = Path.GetFullPath (Path.Combine (artifactsBinDir, "ILLink.RoslynAnalyzer.Tests", configDirectoryName, tfm));
		}

		internal static Task VerifyCodeFix<TAnalyzer, TCodeFix> (
			string source,
			string fixedSource,
			DiagnosticResult[] baselineExpected,
			DiagnosticResult[] fixedExpected,
			int? numberOfIterations = null)
			where TAnalyzer : DiagnosticAnalyzer, new()
			where TCodeFix : CodeFixProvider, new()
		{
			string analyzerAttribute = GetAttributeDefinition<TAnalyzer> ();
			string codeFixAttribute = GetAttributeDefinition<TCodeFix> ();
			var attributeDefinitions = analyzerAttribute;
			if (string.Equals (analyzerAttribute, codeFixAttribute) == false) {
				attributeDefinitions += codeFixAttribute;
			}

			var test = new CSharpCodeFixVerifier<TAnalyzer, TCodeFix>.Test {
				TestCode = source + attributeDefinitions,
				FixedCode = fixedSource + attributeDefinitions,
			};
			test.ExpectedDiagnostics.AddRange (baselineExpected);
			var analyzerMSBuildPropertyOption = GetAnalyzerMSBuildPropertyOption<TAnalyzer> ();
			test.TestState.AnalyzerConfigFiles.Add (
						("/.editorconfig", SourceText.From (@$"
is_global = true
build_property.{analyzerMSBuildPropertyOption} = true"
)));
			if (numberOfIterations != null) {
				test.NumberOfIncrementalIterations = numberOfIterations;
				test.NumberOfFixAllIterations = numberOfIterations;
			}
			test.FixedState.ExpectedDiagnostics.AddRange (fixedExpected);
			return test.RunAsync ();
		}

		internal static Task VerifyDiagnostic<TAnalyzer> (
			string source,
			DiagnosticResult[] baselineExpected)
			where TAnalyzer : DiagnosticAnalyzer, new() =>
			CSharpAnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync (
				source,
				TestCaseUtils.UseMSBuildProperties (GetAnalyzerMSBuildPropertyOption<TAnalyzer> ()),
				baselineExpected);

		internal static DiagnosticResult GetDiagnosticResult<TAnalyzer> ()
			where TAnalyzer : DiagnosticAnalyzer, new()
		{
			return CSharpAnalyzerVerifier<TAnalyzer>.Diagnostic (GetDiagnosticId<TAnalyzer> ());
		}

		private static Dictionary<Type, string> diagnosticDict = new Dictionary<Type, string>
		{
			{typeof(RequiresUnreferencedCodeAnalyzer), RequiresUnreferencedCodeAnalyzer.DiagnosticId},
			{typeof(RequiresAssemblyFilesAnalyzer), RequiresAssemblyFilesAnalyzer.IL3002}
		};

		private static string GetDiagnosticId<TAnalyzer> () where TAnalyzer : DiagnosticAnalyzer
		{
			if (diagnosticDict.ContainsKey (typeof (TAnalyzer))) {
				return diagnosticDict[typeof (TAnalyzer)];
			} else {
				throw new ArgumentException ($"Couldn't retrieve diagnostic id data for unrecognized Analyzer {typeof (TAnalyzer).Name}");
			}
		}

		private static Dictionary<Type, string> attributeDefinitionDict = new Dictionary<Type, string>
		{
			{typeof(RequiresUnreferencedCodeAnalyzer), requiresUnreferencedCodeAttributeDefinition},
			{typeof(RequiresUnreferencedCodeCodeFixProvider), requiresUnreferencedCodeAttributeDefinition},
			{typeof(RequiresAssemblyFilesAnalyzer), requiresAssemblyFilesAttributeDefinition},
			{typeof(UnconditionalSuppressMessageCodeFixProvider), unconditionalSuppressMessageAttributeDefinition}
		};

		private static string GetAttributeDefinition<T> ()
		{
			if (attributeDefinitionDict.ContainsKey (typeof (T))) {
				return attributeDefinitionDict[typeof (T)];
			} else {
				throw new ArgumentException ($"Couldn't retrieve attribute information for unrecognized type {typeof (T).Name}");
			}
		}

		private static Dictionary<Type, string> msbuildOptionsDict = new Dictionary<Type, string>
		{
			{typeof(RequiresUnreferencedCodeAnalyzer), MSBuildPropertyOptionNames.EnableTrimAnalyzer},
			{typeof(RequiresAssemblyFilesAnalyzer), MSBuildPropertyOptionNames.EnableSingleFileAnalyzer}
		};

		private static string GetAnalyzerMSBuildPropertyOption<TAnalyzer> () where TAnalyzer : DiagnosticAnalyzer
		{
			if (msbuildOptionsDict.ContainsKey (typeof (TAnalyzer))) {
				return msbuildOptionsDict[typeof (TAnalyzer)];
			} else {
				throw new ArgumentException ($"Couldn't retrieve the msbuild option for unrecognized Analyzer {typeof (TAnalyzer).Name}");
			}
		}
	}
}
