using System.Collections.Generic;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseTestSandbox
	{
		protected readonly TestCase _testCase;

		protected BaseTestSandbox(TestCase testCase)
		{
			_testCase = testCase;
		}

		public abstract NPath InputDirectory { get; }

		public abstract NPath OutputDirectory { get; }

		public abstract IEnumerable<NPath> SourceFiles { get; }

		public abstract IEnumerable<NPath> References { get; }

		public abstract IEnumerable<NPath> LinkXmlFiles { get; }

		public abstract void Populate(BaseTestCaseMetadaProvider metadataProvider);
	}
}
