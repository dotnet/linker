using System.Collections;
using System.Collections.Generic;
using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Utils;

namespace Mono.Linker.Tests.XUnitIntegration
{
	public class TestDatabase : IEnumerable<object>
	{
		public IEnumerator<object> GetEnumerator()
		{
			var testCases = new TestCaseCollector(PathUtils.RootTestCaseDirectory, PathUtils.TestCaseAssemblyPath);
			foreach (var testCase in testCases.Collect())
				yield return new object[] { testCase };
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
