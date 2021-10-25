// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
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
		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (RequiresCapability))]
		public void RequiresCapability (string m)
		{
			RunTest (nameof (RequiresCapability), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (Interop))]
		public void Interop (string m)
		{
			RunTest (nameof (Interop), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer));
		}

		[Theory]
		[MemberData (nameof (TestCaseUtils.GetTestData), parameters: nameof (DataFlow))]
		public void DataFlow (string m)
		{
			var shouldRun = (TestCase testCase) => {
				var testSyntaxRoot = testCase.MemberSyntax.SyntaxTree.GetRoot ();
				var testCaseClass = testSyntaxRoot.DescendantNodes ().OfType<ClassDeclarationSyntax> ().First ();
				// Double-check that this is the right class. It should have a Main() method.
				var testCaseMain = testCaseClass.DescendantNodes ().OfType<MethodDeclarationSyntax> ().First ();
				if (testCaseMain.Identifier.ValueText != "Main")
					throw new NotImplementedException ();

				switch (testCaseClass.Identifier.ValueText) {
				case "MemberTypesRelationships":
				case "MethodParametersDataFlow":
				case "MethodReturnParameterDataFlow":
					return true;
				// case "AnnotatedMembersAccessedViaReflection":
				// case "AssemblyQualifiedNameDataflow":
				// case "ByRefDataflow":
				// case "DynamicDependencyDataflow":
				// case "EmptyArrayIntrinsicsDataFlow":
				// case "FieldDataFlow":
				// case "GenericParameterDataFlow":
				// case "GetInterfaceDataFlow":
				// case "GetNestedTypeOnAllAnnotatedType":
				// case "GetTypeDataFlow":
				// case "IReflectDataflow":
				case "LocalDataFlow": {
						if (m is not MethodDeclarationSyntax method)
							return;
						switch (method.Identifier.ValueText) {
						// These cases still fail:
						// MergeTry case doesn't track control flow for throw out of a try block,
						// only sees value assigned after the throw, doesn't warn on RequirePublicMethods. 
						case "TestBranchMergeTry":
						case "TestBranchMergeCatch":
						case "TestBranchMergeFinally":
						// Analyzer gets these right even though linker doesn't:
						case "TestBranchGoto":
						case "TestBranchIf":
						case "TestBranchIfElse":
						case "TestBranchSwitch":
						// Analyzer produces no warnings, linker extraneous warnings
						// But not sure if analyzer just *happens* to be correct
						// so need to validate these again later.
						case "TestBranchTry":
						case "TestBranchCatch":
						case "TestBranchFinally":
						// Analyzer gets these cases right, but testcases expect
						// the incorrect linker behavior.
						case "TestBackwardsEdgeLoop":
						case "TestBackwardsEdgeGoto":
							return;
						}
						break;
					}
			default:
				return;				default:
					return false;
				}
			};

			RunTest (nameof (DataFlow), m, UseMSBuildProperties (MSBuildPropertyOptionNames.EnableTrimAnalyzer), shouldRun);
		}
	}
}
