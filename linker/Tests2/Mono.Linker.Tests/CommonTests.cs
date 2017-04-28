using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.CoreIntegration;
using Mono.Linker.Tests.NUnitIntegration;
using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class CommonTests
	{
		[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.AllTests))]
		public void AllTestsNUnit(TestCase testCase)
		{
			var runner = new TestRunner(new ObjectFactory(new NUnitAssertions()));
			runner.Run(testCase);
		}

		// Once xunit reference is added, can uncomment this
		//[Theory]
		//[ClassData(typeof(TestCaseTestSource))]
		public void AllTestsXUnit(TestCase testCase)
		{
			var runner = new TestRunner(new ObjectFactory(new XUnitAssertions()));
			runner.Run(testCase);
		}
	}
}
