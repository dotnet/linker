﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class TestCase
	{
		public readonly MemberDeclarationSyntax MemberSyntax;

		private readonly IEnumerable<AttributeSyntax> Attributes;

		public string? Name { get; set; }

		public TestCase (MemberDeclarationSyntax memberSyntax, IEnumerable<AttributeSyntax> attributes)
		{
			MemberSyntax = memberSyntax;
			Attributes = attributes;
		}

		public void Run ((CompilationWithAnalyzers, SemanticModel) compAndModel)
		{
			var testSyntaxTree = MemberSyntax.SyntaxTree;
			var testDependenciesSource = GetTestDependencies (testSyntaxTree)
				.Select (testDependency => CSharpSyntaxTree.ParseText (File.ReadAllText (testDependency)));

			var test = new TestChecker (MemberSyntax, compAndModel);
			test.ValidateAttributes (Attributes);
		}

		public static IEnumerable<string> GetTestDependencies (SyntaxTree testSyntaxTree)
		{
			LinkerTestBase.GetDirectoryPaths (out var rootSourceDir, out _);
			foreach (var attribute in testSyntaxTree.GetRoot ().DescendantNodes ().OfType<AttributeSyntax> ()) {
				var attributeName = attribute.Name.ToString ();
				if (attributeName != "SetupCompileBefore" && attributeName != "SandboxDependency")
					continue;

				var testNamespace = testSyntaxTree.GetRoot ().DescendantNodes ().OfType<NamespaceDeclarationSyntax> ().First ().Name.ToString ();
				var testSuiteName = testNamespace.Substring (testNamespace.LastIndexOf ('.') + 1);
				var args = LinkerTestBase.GetAttributeArguments (attribute);

				switch (attributeName) {
				case "SetupCompileBefore": {
						foreach (var sourceFile in ((ImplicitArrayCreationExpressionSyntax) args["#1"]).DescendantNodes ().OfType<LiteralExpressionSyntax> ())
							yield return Path.Combine (rootSourceDir, testSuiteName, LinkerTestBase.GetStringFromExpression (sourceFile));
						break;
					}
				case "SandboxDependency": {
						var sourceFile = LinkerTestBase.GetStringFromExpression (args["#0"]);
						if (!sourceFile.EndsWith (".cs"))
							throw new NotSupportedException ();
						yield return Path.Combine (rootSourceDir, testSuiteName, sourceFile);
						break;
					}
				default:
					throw new InvalidOperationException ();
				}
			}
		}
	}
}
