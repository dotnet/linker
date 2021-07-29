﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public abstract class TestCaseUtils
	{
		public static readonly ReferenceAssemblies Net6PreviewAssemblies =
			new ReferenceAssemblies (
				"net6.0",
				new PackageIdentity ("Microsoft.NETCore.App.Ref", "6.0.0-preview.7.21368.2"),
				Path.Combine ("ref", "net6.0"))
			.WithNuGetConfigFilePath (Path.Combine (TestCaseUtils.GetRepoRoot (), "NuGet.config"));

		private static ImmutableArray<MetadataReference> s_net6Refs;
		public async static ValueTask<ImmutableArray<MetadataReference>> GetNet6References ()
		{
			if (s_net6Refs.IsDefault) {
				var refs = await Net6PreviewAssemblies.ResolveAsync (null, default);
				ImmutableInterlocked.InterlockedInitialize (ref s_net6Refs, refs);
			}
			return s_net6Refs;
		}

		public static IEnumerable<object[]> GetTestData (string testSuiteName)
		{
			foreach (var testFile in s_testFiles[testSuiteName]) {
				var root = CSharpSyntaxTree.ParseText (File.ReadAllText (testFile)).GetRoot ();

				foreach (var node in root.DescendantNodes ()) {
					if (node is MemberDeclarationSyntax m) {
						var attrs = m.AttributeLists.SelectMany (al => al.Attributes.Where (IsWellKnown)).ToList ();
						if (attrs.Count > 0) {
							yield return new object[] { m, attrs };
						}
					}
					if (node is AccessorDeclarationSyntax a) {
						var attrs = a.AttributeLists.SelectMany (al => al.Attributes.Where (IsWellKnown)).ToList ();
						if (attrs.Count > 0) {
							yield return new object[] { a, attrs };
						}
					}
				}

				static bool IsWellKnown (AttributeSyntax attr)
				{
					switch (attr.Name.ToString ()) {
					// Currently, the analyzer's test infra only understands these attributes when placed on methods and properties.
					case "ExpectedWarning":
					case "LogContains":
					case "LogDoesNotContain":
						var ancestor = attr.Ancestors ().OfType<MemberDeclarationSyntax> ().First ();
						if (ancestor.IsKind (SyntaxKind.MethodDeclaration) || ancestor.IsKind (SyntaxKind.PropertyDeclaration) || ancestor.IsKind (SyntaxKind.GetAccessorDeclaration) || ancestor.IsKind (SyntaxKind.SetAccessorDeclaration))
							return true;
						return false;

					case "UnrecognizedReflectionAccessPattern":
						return true;
					}

					return false;
				}
			}
		}

		public static async Task<Compilation> GetCompilation (string src)
		{
			var srctree = CSharpSyntaxTree.ParseText (src);
			var mdRef = MetadataReference.CreateFromFile (typeof (Mono.Linker.Tests.Cases.Expectations.Metadata.BaseMetadataAttribute).Assembly.Location);
			var comp = CSharpCompilation.Create (
				assemblyName: Guid.NewGuid ().ToString ("N"),
				syntaxTrees: new SyntaxTree[] { srctree },
				references: (await GetNet6References ()).Add (mdRef),
				new CSharpCompilationOptions (OutputKind.DynamicallyLinkedLibrary));

			return comp;
		}

		public static void RunTest<TAnalyzer> (SyntaxNode m, List<AttributeSyntax> attrs, params (string, string)[] MSBuildProperties)
			where TAnalyzer : DiagnosticAnalyzer, new() => RunTest<TAnalyzer> (m, attrs, null, MSBuildProperties);

		public static void RunTest<TAnalyzer> (SyntaxNode m, List<AttributeSyntax> attrs, IEnumerable<MetadataReference>? additionalReferences = null, params (string, string)[] MSBuildProperties)
			where TAnalyzer : DiagnosticAnalyzer, new()
		{
			var test = new TestChecker (m, CSharpAnalyzerVerifier<TAnalyzer>
				.CreateCompilation (m.SyntaxTree.GetRoot ().SyntaxTree, MSBuildProperties, additionalReferences).Result);
			test.ValidateAttributes (attrs);
		}

		public static readonly ImmutableDictionary<string, List<string>> s_testFiles = GetTestFilesByDirName ();

		public static ImmutableDictionary<string, List<string>> GetTestFilesByDirName ()
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

		public static ImmutableDictionary<string, List<string>> GetReferenceFilesByDirName ()
		{
			var builder = ImmutableDictionary.CreateBuilder<string, List<string>> ();

			foreach (var file in GetReferenceFiles ()) {
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

		public static IEnumerable<string> GetReferenceFiles ()
		{
			GetDirectoryPaths (out var rootSourceDir, out _);

			foreach (var subDir in Directory.EnumerateDirectories (rootSourceDir, "*", SearchOption.AllDirectories)) {
				var subDirName = Path.GetFileName (subDir);
				switch (subDirName) {
				case "Dependencies":
					foreach (var file in Directory.EnumerateFiles (subDir, "*.cs"))
						yield return file;
					break;
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

		// Accepts string literal expressions or binary expressions concatenating strings
		public static string GetStringFromExpression (ExpressionSyntax expr, SemanticModel? semanticModel = null)
		{
			if (expr == null)
				return null!;

			switch (expr.Kind ()) {
			case SyntaxKind.AddExpression:
				var addExpr = (BinaryExpressionSyntax) expr;
				return GetStringFromExpression (addExpr.Left) + GetStringFromExpression (addExpr.Right);

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

			case SyntaxKind.TypeOfExpression:
				return semanticModel.GetTypeInfo (expr).Type!.GetDisplayName ();

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

		public static IEnumerable<string> GetTestFiles ()
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
