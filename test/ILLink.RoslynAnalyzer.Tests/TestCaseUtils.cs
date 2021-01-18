﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class TestCaseUtils
	{
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
					var expectedMessageContains = attr.ArgumentList!.Arguments.Select (m => GetStringFromExpr (m.Expression)).ToList ();
					if (!expectedMessageContains[0].StartsWith ("IL"))
						break;
					expectedMessageContains.RemoveAt (0);
					Assert.True (
						filtered.Any (mc => {
							foreach (var expectedMessage in expectedMessageContains) {
								if (!mc.Contains (expectedMessage))
									return false;
							}
							return true;
						}),
					$"Expected to find warning containing:{string.Join (" ", expectedMessageContains.Select (m => "'" + m + "'"))}" +
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

		internal static void GetDirectoryPaths (out string rootSourceDirectory, out string testAssemblyPath, [CallerFilePath] string thisFile = null)
		{

#if DEBUG
			var configDirectoryName = "Debug";
#else
			var configDirectoryName = "Release";
#endif
			const string tfm = "net5.0";

			// working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var artifactsBinDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..");
			rootSourceDirectory = Path.GetFullPath (Path.Combine (artifactsBinDir, "..", "..", "test", "Mono.Linker.Tests.Cases"));
			testAssemblyPath = Path.GetFullPath (Path.Combine (artifactsBinDir, "ILLink.RoslynAnalyzer.Tests", configDirectoryName, tfm));
		}
	}
}
