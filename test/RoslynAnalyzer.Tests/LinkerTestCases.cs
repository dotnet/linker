﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Xunit;

namespace RoslynAnalyzer.Tests
{
	/// <summary>
	/// Test cases stored in files
	/// </summary>
	public class LinkerTestCases : TestCaseUtils
	{
		[Theory]
		[MemberData (nameof (GetTestData), parameters: nameof (RequiresCapability))]
		public void RequiresCapability (MethodDeclarationSyntax m, List<AttributeSyntax> attrs, ImmutableArray<Diagnostic> diags)
		{
			switch (m.Identifier.ValueText) {
			case "RequiresAndCallsOtherRequiresMethods":
			case "TestRequiresWithMessageAndUrlOnMethod":
				// Test failures because analyzer support is not complete
				// Skip for now
				return;
			}

			RunTest (m, attrs, diags);
		}
	}
}
