using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Utils;
using Mono.Linker.Tests.Utils;
using NUnit.Framework;

namespace Mono.Linker.Tests.NUnitIntegration
{
	public class TestDatabase
	{
		public IEnumerable AllTests()
		{
			return AllTestCases(PathUtils.RootTestCaseDirectory);
		}

		public static IEnumerable AllTestCases(NPath rootTestCaseDirectory)
		{
			var testCases = new TestCaseCollector(rootTestCaseDirectory, PathUtils.TestCaseAssemblyPath);
			return MakeTestCasesForProfile(testCases.Collect().ToArray());
		}

		private static IEnumerable<TestCaseData> MakeTestCasesForProfile(TestCase[] testCases)
		{
			foreach (var test in testCases.OrderBy(t => t.DisplayName))
			{
				var data = new TestCaseData(test);
				data.SetName(test.DisplayName);
				yield return data;
			}
		}
	}
}
