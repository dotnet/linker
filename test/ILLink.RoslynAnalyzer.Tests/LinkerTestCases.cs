// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	/// <summary>
	/// Test cases stored in files
	/// </summary>
	public class LinkerTestCases : TestCaseUtils
	{
		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: "RequiresCapability")]
		public void RequiresUnreferencedCodeCapability (MethodDeclarationSyntax m, List<AttributeSyntax> attrs)
		{
			switch (m.Identifier.ValueText) {
			case "MethodWithDuplicateRequiresAttribute":
			case "TestRequiresOnlyThroughReflection":
			case "TestRequiresInMethodFromCopiedAssembly":
			case "TestRequiresThroughReflectionInMethodFromCopiedAssembly":
				return;
			}

			RunTest<RequiresUnreferencedCodeAnalyzer> (m, attrs, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: "RequiresCapability")]
		public void RequiresAssemblyFilesCapability (MethodDeclarationSyntax m, List<AttributeSyntax> attrs)
		{
			switch (m.Identifier.ValueText) {
			case "MethodWithDuplicateRequiresAttribute":
			case "TestRequiresOnlyThroughReflection":
			case "TestRequiresInMethodFromCopiedAssembly":
			case "TestRequiresThroughReflectionInMethodFromCopiedAssembly":
				return;
			}

			RunTest<RequiresAssemblyFilesAnalyzer> (m, attrs, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer));
		}
	}
}
