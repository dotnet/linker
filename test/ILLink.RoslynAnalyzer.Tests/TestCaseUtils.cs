// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using Microsoft.CodeAnalysis.Text;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public abstract class TestCaseUtils
	{
		private static readonly string MonoLinkerTestsCases = "Mono.Linker.Tests.Cases";

		public static readonly ReferenceAssemblies Net6PreviewAssemblies =
			new ReferenceAssemblies (
				"net6.0",
				new PackageIdentity ("Microsoft.NETCore.App.Ref", "6.0.0-preview.7.21368.2"),
				Path.Combine ("ref", "net6.0"))
			.WithNuGetConfigFilePath (Path.Combine (TestCaseUtils.GetRepoRoot (), "NuGet.config"));

		private static ImmutableArray<MetadataReference> s_net6Refs;
		public static async ValueTask<ImmutableArray<MetadataReference>> GetNet6References ()
		{
			if (s_net6Refs.IsDefault) {
				var refs = await Net6PreviewAssemblies.ResolveAsync (null, default);
				ImmutableInterlocked.InterlockedInitialize (ref s_net6Refs, refs);
			}
			return s_net6Refs;
		}

		public static string FindTestDir (string rootDir, string suiteName)
		{
			string[] suiteParts = suiteName.Split ('.');
			string currentDir = rootDir;
			foreach (var suitePart in suiteParts) {
				string dirCandidate = Path.Combine (currentDir, suitePart);
				if (currentDir == rootDir || Directory.Exists (dirCandidate))
					currentDir = dirCandidate;
				else
					currentDir += $".{suitePart}";
			}

			return currentDir;
		}

		public static async Task RunTestFile (string suiteName, string testName, bool allowMissingWarnings, params (string, string)[] msbuildProperties)
		{
			GetDirectoryPaths (out string rootSourceDir, out string testAssemblyPath);
			Debug.Assert (Path.GetFileName (rootSourceDir) == MonoLinkerTestsCases);
			var testDir = FindTestDir (rootSourceDir, suiteName);
			var testPath = Path.Combine (testDir, $"{testName}.cs");
			Assert.True (File.Exists (testPath));
			var tree = SyntaxFactory.ParseSyntaxTree (
				SourceText.From (File.OpenRead (testPath), Encoding.UTF8),
				path: testPath);

			var testDependenciesSource = GetTestDependencies (testDir, tree)
				.Where (f => Path.GetExtension (f) == ".cs")
				.Select (f => SyntaxFactory.ParseSyntaxTree (SourceText.From (File.OpenRead (f))));
			var additionalFiles = GetAdditionalFiles (rootSourceDir, tree);

			var (comp, model, exceptionDiagnostics) = await TestCaseCompilation.CreateCompilation (
					tree,
					msbuildProperties,
					additionalSources: testDependenciesSource,
					additionalFiles: additionalFiles);

			// Note that the exception diagnostics will be empty until the analyzer has run,
			// so be sure to get them after awaiting GetAnalyzerDiagnosticsAsync().
			var diags = (await comp.GetAnalyzerDiagnosticsAsync ()).AddRange (exceptionDiagnostics);

			var testChecker = new TestChecker ((CSharpSyntaxTree) tree, model, diags);
			testChecker.Check (allowMissingWarnings);
		}

		private static IEnumerable<string> GetTestDependencies (string testDir, SyntaxTree testSyntaxTree)
		{
			foreach (var attribute in testSyntaxTree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				var attributeName = attribute.Name.ToString ();
				if (attributeName != "SetupCompileBefore" && attributeName != "SandboxDependency")
					continue;
				var args = LinkerTestBase.GetAttributeArguments (attribute);

				switch (attributeName) {
				case "SetupCompileBefore": {
						var arrayExpression = args["#1"];
						if (arrayExpression is not (ArrayCreationExpressionSyntax or ImplicitArrayCreationExpressionSyntax))
							throw new InvalidOperationException ();
						foreach (var sourceFile in ((ExpressionSyntax) args["#1"]).DescendantNodes ().OfType<LiteralExpressionSyntax> ())
							yield return Path.Combine (testDir, LinkerTestBase.GetStringFromExpression (sourceFile));
						break;
					}
				case "SandboxDependency": {
						var sourceFile = LinkerTestBase.GetStringFromExpression (args["#0"]);
						if (!sourceFile.EndsWith (".cs"))
							throw new NotSupportedException ();
						yield return Path.Combine (testDir, sourceFile);
						break;
					}
				default:
					throw new InvalidOperationException ();
				}
			}
		}

		private static IEnumerable<AdditionalText> GetAdditionalFiles(string rootSourceDir, SyntaxTree tree)
		{
			var resolver = new XmlFileResolver (rootSourceDir);
			foreach (var attribute in tree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				if (attribute.Name.ToString () == nameof (SetupLinkAttributesFile)
					|| (attribute.Name.ToString ().Contains ("SetupCompileResource")
						&& (string?) attribute.ArgumentList?.Arguments[1].ToString () == "\"ILLink.LinkAttributes.xml\"")) {
					var xmlFileName = attribute.ArgumentList?.Arguments[0].ToString ().Trim ('"') ?? "";
					var resolvedPath = resolver.ResolveReference (xmlFileName, rootSourceDir);
					if (resolvedPath != null) {
						var stream = resolver.OpenRead (resolvedPath);
						XmlText text = new ("ILLink.LinkAttributes.xml", stream);
						yield return text;
					}
				}
			}
		}

		public static void GetDirectoryPaths (out string rootSourceDirectory, out string testAssemblyPath)
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

			// Working directory is artifacts/bin/Mono.Linker.Tests/<config>/<tfm>
			var artifactsBinDir = Path.Combine (Directory.GetCurrentDirectory (), "..", "..", "..");
			rootSourceDirectory = Path.GetFullPath (Path.Combine (artifactsBinDir, "..", "..", "test", "Mono.Linker.Tests.Cases"));
			testAssemblyPath = Path.GetFullPath (Path.Combine (artifactsBinDir, "ILLink.RoslynAnalyzer.Tests", configDirectoryName, tfm));
		}

		// Accepts typeof expressions, with a format specifier
		public static string GetStringFromExpression (TypeOfExpressionSyntax expr, SemanticModel semanticModel, SymbolDisplayFormat displayFormat)
		{
			var typeSymbol = semanticModel.GetSymbolInfo (expr.Type).Symbol;
			return typeSymbol?.ToDisplayString (displayFormat) ?? throw new InvalidOperationException ();
		}

		// Accepts string literal expressions or binary expressions concatenating strings
		public static string GetStringFromExpression (ExpressionSyntax expr, SemanticModel? semanticModel = null)
		{
			if (expr == null)
				return null!;

			switch (expr.Kind ()) {
			case SyntaxKind.AddExpression:
				var addExpr = (BinaryExpressionSyntax) expr;
				return GetStringFromExpression (addExpr.Left, semanticModel) + GetStringFromExpression (addExpr.Right, semanticModel);

			case SyntaxKind.InvocationExpression:
				var nameofValue = semanticModel!.GetConstantValue (expr);
				if (nameofValue.HasValue)
					return (nameofValue.Value as string)!;

				return string.Empty;

			case SyntaxKind.StringLiteralExpression:
				var strLiteral = (LiteralExpressionSyntax) expr;
				var token = strLiteral.Token;
				Assert.Equal (SyntaxKind.StringLiteralToken, token.Kind ());
				return token.ValueText;

			default:
				Assert.True (false, "Unsupported expr kind " + expr.Kind ());
				return null!;
			}
		}

		public static Dictionary<string, ExpressionSyntax> GetAttributeArguments (AttributeSyntax attribute)
		{
			Dictionary<string, ExpressionSyntax> arguments = new Dictionary<string, ExpressionSyntax> ();
			int ordinal = 0;
			foreach (var argument in attribute.ArgumentList!.Arguments) {
				string argName;
				if (argument.NameEquals != null) {
					argName = argument.NameEquals.ChildNodes ().OfType<IdentifierNameSyntax> ().First ().Identifier.ValueText;
				} else if (argument.NameColon is NameColonSyntax nameColon) {
					argName = nameColon.Name.Identifier.ValueText;
				} else {
					argName = "#" + ordinal.ToString ();
					ordinal++;
				}
				arguments.Add (argName, argument.Expression);
			}

			return arguments;
		}

		public static (string, string)[] UseMSBuildProperties (params string[] MSBuildProperties)
		{
			return MSBuildProperties.Select (msbp => ($"build_property.{msbp}", "true")).ToArray ();
		}

		public static string GetRepoRoot ()
		{
			return Directory.GetParent (ThisFile ())!.Parent!.Parent!.FullName;

			string ThisFile ([CallerFilePath] string path = "") => path;
		}
	}
}
