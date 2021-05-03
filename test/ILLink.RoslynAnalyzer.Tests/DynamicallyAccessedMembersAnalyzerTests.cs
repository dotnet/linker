// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using VerifyCS = ILLink.RoslynAnalyzer.Tests.CSharpAnalyzerVerifier<
	ILLink.RoslynAnalyzer.DynamicallyAccessedMembersAnalyzer>;

namespace ILLink.RoslynAnalyzer.Tests
{
	public class DynamicallyAccessedMembersAnalyzerTests
	{
		static Task VerifyDynamicallyAccessedMembersAnalyzer (string source, params DiagnosticResult[] expected)
		{
			return VerifyCS.VerifyAnalyzerAsync (source,
				TestCaseUtils.UseMSBuildProperties (MSBuildPropertyOptionNames.EnableSingleFileAnalyzer),
				expected);
		}

		[Fact]
		public Task MethodHasDynamicallyAccessedMembersAttribute ()
		{
			var TestMethodsCannotBeAnnotatedWithDynamicallyAccessedMembersAttribute = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	// IL2041
	[DynamicallyAccessedMembers (DynamicallyAccessedMemberTypes.PublicMethods)]
	int M()
	{
		return 0;
	}
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TestMethodsCannotBeAnnotatedWithDynamicallyAccessedMembersAttribute,
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.M()"));
		}

		[Fact]
		public Task MethodParameterAnnotationsDoesNotMatchReturnValueAnnotations ()
		{
			var TestMethodParameterAnnotationsDoesNotMatchReturnValueAnnotations = @"
using System.Diagnostics.CodeAnalysis;

class C
{
	[return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicConstructors)]
	private Type M(
		[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
		Type parameter)
	{
		return null;
	}
}";

			return VerifyDynamicallyAccessedMembersAnalyzer (TestMethodParameterAnnotationsDoesNotMatchReturnValueAnnotations,
				VerifyCS.Diagnostic ().WithSpan (8, 17, 8, 21).WithArguments ("C.ReturnNonPublicConstructorsFailure ()"));
		}
	}
}
