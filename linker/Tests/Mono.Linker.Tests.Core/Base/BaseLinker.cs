using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseLinker
	{
		// TODO by Mike : Remove if does not end up being used
		protected readonly TestCase _testCase;

		protected BaseLinker(TestCase testCase)
		{
			_testCase = testCase;
		}

		public abstract void Link(string[] args);
	}
}
