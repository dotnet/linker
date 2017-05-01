using Mono.Linker.Tests.Core;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestsCases
{
	public class TestSuites
	{
		[TestFixture]
		public class CommonTests
		{
			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.BasicTests))]
			public void BasicTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.VirtualMethodsTests))]
			public void VirtualMethodTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.XmlTests))]
			public void XmlTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.AttributeTests))]
			public void AttributesTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.GenericsTests))]
			public void GenericsTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.StaticsTests))]
			public void StaticsTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.CoreLinkTests))]
			public void CoreLinkTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.InteropTests))]
			public void InteropTests(TestCase testCase)
			{
				Run(testCase);
			}

			[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.UngroupedTests))]
			public void UngroupedTests(TestCase testCase)
			{
				Run(testCase);
			}

			private void Run(TestCase testCase)
			{
				var runner = new TestRunner(new ObjectFactory());
				runner.Run(testCase);
			}
		}
	}
}
