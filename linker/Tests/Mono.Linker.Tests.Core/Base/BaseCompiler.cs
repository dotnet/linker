using System.Collections.Generic;

namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseCompiler
	{
		protected readonly TestCase _testCase;

		protected BaseCompiler(TestCase testCase)
		{
			_testCase = testCase;
		}

		public abstract ManagedCompilationResult CompileTestIn(BaseTestSandbox sandbox, IEnumerable<string> referencesExternalToSandbox);
	}
}
