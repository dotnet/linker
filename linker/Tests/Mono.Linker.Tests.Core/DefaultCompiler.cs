using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Core.Base;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core
{
	public class DefaultCompiler : BaseCompiler
	{
		public DefaultCompiler(TestCase testCase)
			: base(testCase)
		{
		}

		public override ManagedCompilationResult CompileTestIn(BaseTestSandbox sandbox, IEnumerable<string> referencesExternalToSandbox)
		{
			var compilerOptions = CreateCompilerOptions(sandbox, referencesExternalToSandbox);
			var provider = CodeDomProvider.CreateProvider("C#");
			var result = provider.CompileAssemblyFromFile(compilerOptions, sandbox.SourceFiles.Select(f => f.ToString()).ToArray());
			if (!result.Errors.HasErrors)
				return new ManagedCompilationResult(compilerOptions.OutputAssembly.ToNPath());

			var errors = new StringBuilder();
			foreach (var error in result.Errors)
				errors.AppendLine(error.ToString());
			throw new Exception("Compilation errors: " + errors);
		}

		protected virtual CompilerParameters CreateCompilerOptions(BaseTestSandbox sandbox, IEnumerable<string> referencesExternalToSandbox)
		{
			var outputPath = sandbox.InputDirectory.Combine("test.exe");

			var compilerParameters = new CompilerParameters
			{
				OutputAssembly = outputPath.ToString(),
				GenerateExecutable = true
			};

			compilerParameters.ReferencedAssemblies.AddRange(referencesExternalToSandbox.ToArray());
			compilerParameters.ReferencedAssemblies.AddRange(sandbox.References.Select(r => r.ToString()).ToArray());

			return compilerParameters;
		}
	}
}
