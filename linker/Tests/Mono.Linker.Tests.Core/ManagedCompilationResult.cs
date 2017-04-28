using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class ManagedCompilationResult
	{
		public ManagedCompilationResult(NPath assemblyPath)
		{
			AssemblyPath = assemblyPath;
		}

		public NPath AssemblyPath { get; }
	}
}
