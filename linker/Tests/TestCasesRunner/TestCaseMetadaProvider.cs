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
			return new TestCaseLinkerOptions {
				CoreLink = GetOptionAttributeValue (nameof (CoreLinkAttribute), "skip"),
				Il8n = GetOptionAttributeValue (nameof (Il8nAttribute), string.Empty),
				IncludeBlacklistStep = GetOptionAttributeValue (nameof (IncludeBlacklistStepAttribute), false)
			};
		}

		public virtual IEnumerable<string> GetReferencedAssemblies (NPath workingDirectory)
		{
			yield return workingDirectory.Combine ("Mono.Linker.Tests.Cases.Expectations.dll").ToString ();
			yield return "mscorlib.dll";

			foreach (var referenceAttr in _testCaseTypeDefinition.CustomAttributes.Where (attr => attr.AttributeType.Name == nameof (ReferenceAttribute))) {
				switch (referenceAttr.ConstructorArguments.First ().Value) {
				case string s:
					if (referenceAttr.ConstructorArguments.Count () == 2 && referenceAttr.ConstructorArguments[1].Value is true)
						yield return workingDirectory.Combine (s).ToString ();
					else
						yield return s;
					
					continue;
				case CustomAttributeArgument [] ca:
					yield return workingDirectory.Combine (ca.Select (l => l.Value as string).ToArray ()).ToString ();
					continue;
				}
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

		T GetOptionAttributeValue<T> (string attributeName, T defaultValue)
		{
			var attribute = _testCaseTypeDefinition.CustomAttributes.FirstOrDefault (attr => attr.AttributeType.Name == attributeName);
			if (attribute != null)
				return (T) attribute.ConstructorArguments.First ().Value;

			return defaultValue;
		}
	}
}