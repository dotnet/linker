using Mono.Cecil;

namespace Mono.Linker.Tests.Core.Base
{
	public abstract class BaseObjectFactory
	{
		public virtual BaseTestSandbox CreateSandbox(TestCase testCase)
		{
			return new DefaultTestSandbox(testCase);
		}

		public virtual BaseCompiler CreateCompiler(TestCase testCase)
		{
			return new DefaultCompiler(testCase);
		}

		public abstract BaseLinker CreateLinker(TestCase testCase);

		public virtual BaseChecker CreateChecker(TestCase testCase)
		{
			return new DefaultChecker(testCase);
		}

		public virtual BaseTestCaseMetadaProvider CreateMetadatProvider(TestCase testCase, AssemblyDefinition fullTestCaseAssemblyDefinition)
		{
			return new DefaultTestCaseMetadaProvider(testCase, fullTestCaseAssemblyDefinition);
		}

		public virtual BaseLinkerArgumentBuilder CreateLinkerArgumentBuilder()
		{
			return new DefaultLinkerArgumentBuilder();
		}
	}
}
