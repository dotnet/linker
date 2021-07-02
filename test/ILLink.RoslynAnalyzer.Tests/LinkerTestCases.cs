﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	/// <summary>
	/// Test cases stored in files
	/// </summary>
	public class LinkerTestCases : TestCaseUtils
	{
		private static Lazy<IEnumerable<MetadataReference>> _additionalReferences = new Lazy<IEnumerable<MetadataReference>> (LoadDependencies);

		private static IEnumerable<MetadataReference> LoadDependencies ()
		{
			List<MetadataReference> additionalReferences = new List<MetadataReference> ();
			var s_refFiles = GetReferenceFilesByDirName ();
			foreach (var refFile in s_refFiles["Dependencies"]) {
				if (refFile.Contains ("RequiresCapability"))
					additionalReferences.Add (CSharpAnalyzerVerifier<RequiresUnreferencedCodeAnalyzer>.GetCompilation (File.ReadAllText (refFile)).Result.EmitToImageReference ());
			}
			return additionalReferences;
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: "RequiresCapability")]
		public void RequiresUnreferencedCodeCapability (SyntaxNode m, List<AttributeSyntax> attrs) =>
			RunTest<RequiresUnreferencedCodeAnalyzer> (m, attrs, _additionalReferences.Value, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: "RequiresCapability")]
		public void RequiresAssemblyFilesCapability (SyntaxNode m, List<AttributeSyntax> attrs) =>
			RunTest<RequiresAssemblyFilesAnalyzer> (m, attrs, _additionalReferences.Value, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer));
	}
}
