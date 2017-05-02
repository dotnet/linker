using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Customizable;
using NUnit.Framework;

namespace Mono.Linker.Tests.TestsCases
{
	[TestFixture]
	public class TestSuites
	{
		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.BasicTests))]
		public void BasicTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.VirtualMethodsTests))]
		public void VirtualMethodTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.XmlTests))]
		public void XmlTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.AttributeTests))]
		public void AttributesTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.GenericsTests))]
		public void GenericsTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.StaticsTests))]
		public void StaticsTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.CoreLinkTests))]
		public void CoreLinkTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.InteropTests))]
		public void InteropTests (TestCase testCase)
		{
			Run (testCase);
		}

		[TestCaseSource(typeof(TestDatabase), nameof(TestDatabase.ReferencesTests))]
		public void ReferencesTests(TestCase testCase)
		{
			Run(testCase);
		}

		[TestCaseSource (typeof (TestDatabase), nameof (TestDatabase.OtherTests))]
		public void OtherTests (TestCase testCase)
		{
			Run (testCase);
		}

		protected virtual void Run (TestCase testCase)
		{
			var runner = new TestRunner (new ObjectFactory ());
			runner.Run (testCase);
		}
	}
}
