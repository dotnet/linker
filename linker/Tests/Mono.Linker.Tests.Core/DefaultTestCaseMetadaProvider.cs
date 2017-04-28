using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Core.Base;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class DefaultTestCaseMetadaProvider : BaseTestCaseMetadaProvider
	{
		protected readonly TypeDefinition _testCaseTypeDefinition;

		public DefaultTestCaseMetadaProvider(TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
			: base(testCase, fullTestCaseAssemblyDefinition)
		{
			// The test case types are never nested so we don't need to worry about that
			_testCaseTypeDefinition = FullTestCaseAssemblyDefinition.MainModule.GetType(_testCase.FullTypeName);

			if (_testCaseTypeDefinition == null)
				throw new InvalidOperationException($"Could not find the type definition for {_testCase.Name} in {_testCase.SourceFile}");
		}

		public override TestCaseLinkerOptions GetLinkerOptions()
		{
			// This will end up becoming more complicated as we get into more complex test cases that require additional
			// data
			return new TestCaseLinkerOptions { CoreLink = "skip" };
		}

		public override NPath ProfileDirectory
		{
			get
			{
				throw new NotImplementedException();
			}
		}

		public override IEnumerable<string> GetReferencedAssemblies()
		{
			yield return "mscorlib.dll";
		}

		public override IEnumerable<NPath> GetExtraLinkerSearchDirectories()
		{
			yield break;
		}

		public override bool IsIgnored(out string reason)
		{
			if (_testCaseTypeDefinition.HasAttribute(nameof(IgnoreTestCaseAttribute)))
			{
				// TODO by Mike : Implement obtaining the real reason
				reason = "TODO : Need to implement parsing reason";
				return true;
			}

			reason = null;
			return false;
		}

		public override IEnumerable<NPath> AdditionalFilesToSandbox()
		{
			foreach (var attr in _testCaseTypeDefinition.CustomAttributes)
			{
				if(attr.AttributeType.Name != nameof(SandboxDependencyAttribute))
					continue;

				var relativeDepPath = ((string)attr.ConstructorArguments.First().Value).ToNPath();
				yield return _testCase.SourceFile.Parent.Combine(relativeDepPath);
			}
		}
	}
}
