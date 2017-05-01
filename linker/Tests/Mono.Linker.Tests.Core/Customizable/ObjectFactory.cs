using Mono.Cecil;

namespace Mono.Linker.Tests.Core.Customizable
{
	public class ObjectFactory
	{
		public virtual DefaultTestSandbox CreateSandbox(TestCase testCase)
		{
			return new DefaultTestSandbox(testCase);
		}

		public virtual DefaultCompiler CreateCompiler()
		{
			return new DefaultCompiler();
		}

		public virtual DefaultLinker CreateLinker()
		{
			return new DefaultLinker();
		}

		public virtual DefaultChecker CreateChecker()
		{
			return new DefaultChecker();
		}

		public virtual DefaultTestCaseMetadaProvider CreateMetadatProvider(TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			return new DefaultTestCaseMetadaProvider(testCase, fullTestCaseAssemblyDefinition);
		}

		public virtual DefaultLinkerArgumentBuilder CreateLinkerArgumentBuilder()
		{
			return new DefaultLinkerArgumentBuilder();
		}
	}
}
