using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Core.Utils;

namespace Mono.Linker.Tests.Core.Customizable
{
	public class DefaultCompiler
	{
		public virtual ManagedCompilationResult CompileTestIn(DefaultTestSandbox sandbox, IEnumerable<string> referencesExternalToSandbox)
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

		protected virtual CompilerParameters CreateCompilerOptions(DefaultTestSandbox sandbox, IEnumerable<string> referencesExternalToSandbox)
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
