using System.Linq;
using System.Collections.Generic;
using NUnit.Framework;
using Mono.Linker.Tests.Core;
using System.Runtime.CompilerServices;
using System.IO;

namespace Mono.Linker.Tests.TestsCases
{
	public class TestDatabase
	{
		public static IEnumerable<TestCaseData> AllTests ()
		{
			return AllTestCases ();
		}

		static IEnumerable<TestCaseData> AllTestCases ([CallerFilePath] string thisFile = null)
		{
			var thisDirectory = Path.GetDirectoryName (thisFile);
			var src_root = Path.GetFullPath (Path.Combine (thisDirectory, "..", "Mono.Linker.Tests.Cases"));

			// TODO: Why? We don't need the assembly at all
			var assembly_path = Path.GetFullPath (Path.Combine (src_root, "bin", "Debug", "Mono.Linker.Tests.Cases.dll"));

			var testCases = new TestCaseCollector (src_root, assembly_path);
			foreach (var test in testCases.Collect ().OrderBy (t => t.DisplayName)) {
				var data = new TestCaseData (test);
				data.SetName (test.DisplayName);
				yield return data;
			}
		}
	}
}
