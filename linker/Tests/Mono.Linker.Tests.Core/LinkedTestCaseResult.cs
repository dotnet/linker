using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core {
	public class LinkedTestCaseResult {
		public readonly TestCase TestCase;
		public readonly NPath InputAssemblyPath;
		public readonly NPath OutputAssemblyPath;

		public LinkedTestCaseResult (TestCase testCase, NPath inputAssemblyPath, NPath outputAssemblyPath)
		{
			TestCase = testCase;
			InputAssemblyPath = inputAssemblyPath;
			OutputAssemblyPath = outputAssemblyPath;
		}
	}
}