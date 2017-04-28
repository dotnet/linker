using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Base
{
	/// <summary>
	/// A class that handles obtaining information about a test case
	/// </summary>
	public abstract class BaseTestCaseMetadaProvider
	{
		protected readonly TestCase _testCase;
		private readonly AssemblyDefinition _fullTestCaseAssemblyDefinition;

		protected BaseTestCaseMetadaProvider(TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			_testCase = testCase;
			_fullTestCaseAssemblyDefinition = fullTestCaseAssemblyDefinition;
		}

		protected AssemblyDefinition FullTestCaseAssemblyDefinition => _fullTestCaseAssemblyDefinition;

		// TODO by Mike : Doesn't feel like the best home for this...
		public abstract NPath ProfileDirectory { get; }

		public abstract IEnumerable<string> GetReferencedAssemblies();

		public abstract IEnumerable<NPath> GetExtraLinkerSearchDirectories();

		public abstract TestCaseLinkerOptions GetLinkerOptions();

		public abstract bool IsIgnored(out string reason);

		public abstract IEnumerable<NPath> AdditionalFilesToSandbox();
	}
}
