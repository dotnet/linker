namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseChecker
	{
		protected readonly TestCase _testCase;

		protected BaseChecker(TestCase testCase, BaseAssertions assertions)
		{
			_testCase = testCase;
			Assert = assertions;
		}

		public abstract void Check(LinkedTestCaseResult linkResult);

		protected BaseAssertions Assert { get; }
	}
}
