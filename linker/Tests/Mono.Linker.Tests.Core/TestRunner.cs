using System;
using Mono.Cecil;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.Core
{
	public class TestRunner
	{
		private readonly BaseObjectFactory _factory;

		public TestRunner(BaseObjectFactory factory)
		{
			_factory = factory;
		}

		public void Run(TestCase testCase)
		{
			using (var fullTestCaseAssemblyDefinition = AssemblyDefinition.ReadAssembly(testCase.OriginalTestCaseAssemblyPath.ToString()))
			{
				var assertions = _factory.CreateAssertions();
				var metadataProvider = _factory.CreateMetadatProvider(testCase, fullTestCaseAssemblyDefinition);

				string ignoreReason;
				if (metadataProvider.IsIgnored(out ignoreReason))
					assertions.Ignore(ignoreReason);

				var sandbox = Sandbox(testCase, metadataProvider);
				var compilationResult = Compile(testCase, sandbox, metadataProvider);
				PrepForLink(sandbox, compilationResult);
				var linkResult = Link(testCase, sandbox, compilationResult, metadataProvider);
				Check(testCase, assertions, linkResult);
			}
		}

		private BaseTestSandbox Sandbox(TestCase testCase, BaseTestCaseMetadaProvider metadataProvider)
		{
			var sandbox = _factory.CreateSandbox(testCase);
			sandbox.Populate(metadataProvider);
			return sandbox;
		}

		private ManagedCompilationResult Compile(TestCase testCase, BaseTestSandbox sandbox, BaseTestCaseMetadaProvider metadataProvider)
		{
			var compiler = _factory.CreateCompiler(testCase);
			return compiler.CompileTestIn(sandbox, metadataProvider.GetReferencedAssemblies());
		}

		private void PrepForLink(BaseTestSandbox sandbox, ManagedCompilationResult compilationResult)
		{
			var entryPointLinkXml = sandbox.InputDirectory.Combine("entrypoint.xml");
			LinkXmlHelpers.WriteXmlFileToPreserveEntryPoint(compilationResult.AssemblyPath, entryPointLinkXml);
		}

		private LinkedTestCaseResult Link(TestCase testCase, BaseTestSandbox sandbox, ManagedCompilationResult compilationResult, BaseTestCaseMetadaProvider metadataProvider)
		{
			var linker = _factory.CreateLinker(testCase);
			var builder = _factory.CreateLinkerArgumentBuilder();
			var caseDefinedOptions = metadataProvider.GetLinkerOptions();

			builder.AddOutputDirectory(sandbox.OutputDirectory);
			foreach(var linkXmlFile in sandbox.LinkXmlFiles)
				builder.AddLinkXmlFile(linkXmlFile);

			builder.AddSearchDirectory(sandbox.InputDirectory);
			foreach (var extraSearchDir in metadataProvider.GetExtraLinkerSearchDirectories())
				builder.AddSearchDirectory(extraSearchDir);

			builder.AddCoreLink(caseDefinedOptions.CoreLink);

			linker.Link(builder.ToArgs());

			return new LinkedTestCaseResult { InputAssemblyPath = compilationResult.AssemblyPath, LinkedAssemblyPath = sandbox.OutputDirectory.Combine(compilationResult.AssemblyPath.FileName) };
		}

		private void Check(TestCase testCase, BaseAssertions assertions, LinkedTestCaseResult linkResult)
		{
			var checker = _factory.CreateChecker(testCase, assertions);

			checker.Check(linkResult);
		}
	}
}
