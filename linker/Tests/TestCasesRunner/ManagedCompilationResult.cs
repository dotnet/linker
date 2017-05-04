using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class ManagedCompilationResult {
		public ManagedCompilationResult (NPath assemblyPath)
		{
			AssemblyPath = assemblyPath;
		}

		public NPath AssemblyPath { get; }
	}
}