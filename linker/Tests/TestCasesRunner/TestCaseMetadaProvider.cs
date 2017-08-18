﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Extensions;
using Mono.Linker.Tests.TestCases;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseMetadaProvider {
		protected readonly TestCase _testCase;
		protected readonly AssemblyDefinition _fullTestCaseAssemblyDefinition;
		protected readonly TypeDefinition _testCaseTypeDefinition;

		public TestCaseMetadaProvider (TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			_testCase = testCase;
			_fullTestCaseAssemblyDefinition = fullTestCaseAssemblyDefinition;
			// The test case types are never nested so we don't need to worry about that
			_testCaseTypeDefinition = fullTestCaseAssemblyDefinition.MainModule.GetType (_testCase.ReconstructedFullTypeName);

			if (_testCaseTypeDefinition == null)
				throw new InvalidOperationException ($"Could not find the type definition for {_testCase.Name} in {_testCase.SourceFile}");
		}

		public virtual TestCaseLinkerOptions GetLinkerOptions ()
		{
			return new TestCaseLinkerOptions
			{
				CoreLink = GetOptionAttributeValue (nameof (CoreLinkAttribute), "skip"),
				Il8n = GetOptionAttributeValue (nameof (Il8nAttribute), string.Empty),
				IncludeBlacklistStep = GetOptionAttributeValue (nameof (IncludeBlacklistStepAttribute), false),
				KeepTypeForwarderOnlyAssemblies = GetOptionAttributeValue (nameof (KeepTypeForwarderOnlyAssembliesAttribute), string.Empty)
			};
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
			yield return "mscorlib.dll";

			foreach (var referenceAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (ReferenceAttribute))) {
				yield return (string) referenceAttr.ConstructorArguments.First ().Value;
			}
		}

		public virtual IEnumerable<NPath> GetExtraLinkerSearchDirectories ()
		{
			yield break;
		}

		public bool IsIgnored (out string reason)
		{
			var ignoreAttribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == nameof (IgnoreTestCaseAttribute));
			if (ignoreAttribute != null) {
				reason = (string)ignoreAttribute.ConstructorArguments.First ().Value;
				return true;
			}

			reason = null;
			return false;
		}

		public virtual IEnumerable<NPath> AdditionalFilesToSandbox ()
		{
			foreach (var attr in _testCaseTypeDefinition.CustomAttributes) {
				if (attr.AttributeType.Name != nameof (SandboxDependencyAttribute))
					continue;

				var relativeDepPath = ((string) attr.ConstructorArguments.First ().Value).ToNPath ();
				yield return _testCase.SourceFile.Parent.Combine (relativeDepPath);
			}
		}

		public virtual IEnumerable<CompileAssemblyInfo> GetCompileAssembliesBefore ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (CompileAssemblyBeforeAttribute))
				.Select (CreateCompileAssemblyInfo);
		}

		public virtual IEnumerable<CompileAssemblyInfo> GetCompileAssembliesAfter ()
		{
			return _testCaseTypeDefinition.CustomAttributes
				.Where (attr => attr.AttributeType.Name == nameof (CompileAssemblyAfterAttribute))
				.Select (CreateCompileAssemblyInfo);
		}

		public virtual IEnumerable<string> GetDefines ()
		{
			return _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (DefineAttribute))
				.Select(attr => (string) attr.ConstructorArguments.First ().Value);
		}

		T GetOptionAttributeValue<T> (string attributeName, T defaultValue)
		{
			var attribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == attributeName);
			if (attribute != null)
				return (T) attribute.ConstructorArguments.First ().Value;

			return defaultValue;
		}

		private CompileAssemblyInfo CreateCompileAssemblyInfo (CustomAttribute attribute)
		{
			return new CompileAssemblyInfo
			{
				OutputName = (string) attribute.ConstructorArguments [0].Value,
				SourceFiles = ((CustomAttributeArgument []) attribute.ConstructorArguments [1].Value).Select (arg => _testCase.SourceFile.Parent.Combine (arg.Value.ToString ())).ToArray (),
				References = ((CustomAttributeArgument []) attribute.ConstructorArguments [2].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				Defines = ((CustomAttributeArgument []) attribute.ConstructorArguments [3].Value)?.Select (arg => arg.Value.ToString ()).ToArray (),
				AddAsReference = attribute.ConstructorArguments.Count >= 5 ? (bool) attribute.ConstructorArguments [4].Value : true
			};
		}
	}
}