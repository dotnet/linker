namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseChecker
	{
		protected readonly TestCase _testCase;

		protected BaseChecker(TestCase testCase)
		{
			_testCase = testCase;
		}

		public abstract void Check(LinkedTestCaseResult linkResult);
	}
}
