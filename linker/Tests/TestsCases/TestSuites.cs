using Mono.Linker.Tests.Core;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestsCases
{
	public class TestSuites
	{
		[TestFixture]
		public class CommonTests
		{
			[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.AllTests))]
			public void AllTests (TestCase testCase)
			{
				var runner = new TestRunner (new ObjectFactory (new NUnitAssertions ()));
				runner.Run (testCase);
			}
		}
	}
}
