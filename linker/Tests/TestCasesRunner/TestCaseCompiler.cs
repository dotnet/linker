﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Extensions;

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseCompiler {
		protected readonly TestCaseMetadaProvider _unityMetadataProvider;
		protected readonly TestCaseSandbox _sandbox;

		public TestCaseCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider unityMetadataProvider)
		{
			_sandbox = sandbox;
			_unityMetadataProvider = unityMetadataProvider;
		}

		public virtual NPath CompileTestIn (NPath outputDirectory, string outputName, IEnumerable<string> sourceFiles, IEnumerable<string> references, IEnumerable<string> defines)
		{
			var originalReferences = references.ToArray ();
			var originalDefines = defines == null ? new string[] { } : defines.ToArray ();

			Prepare (outputDirectory);

			var compiledReferences = CompileBeforeTestAssemblies (outputDirectory, originalReferences, originalDefines);
			var allTestCaseReferences = originalReferences.Concat (compiledReferences.Select (r => r.ToString ())).ToArray ();

			var testAssembly = CompileTestCase (outputDirectory, outputName, sourceFiles.ToArray (), allTestCaseReferences, originalDefines.Concat (_unityMetadataProvider.GetDefines ()).ToArray ());

			// The compile after step is used by tests to mess around with the input to the linker.  Generally speaking, it doesn't seem like we would ever want to mess with the
			// expectations assemblies because this would undermine our ability to inspect them for expected results during ResultChecking.  The UnityLinker UnresolvedHandling tests depend on this
			// behavior of skipping the after test compile
			if (outputDirectory != _sandbox.ExpectationsDirectory)
				CompileAfterTestAssemblies (outputDirectory, originalReferences, originalDefines);

			return testAssembly;
		}

		protected virtual void Prepare (NPath outputDirectory)
		{
		}

		protected virtual NPath CompileTestCase (NPath outputDirectory, string outputName, string[] sourceFiles, string[] references, string[] defines)
		{
			var compilerOptions = CreateCompilerOptions (outputDirectory.Combine (outputName), references, defines);
			var provider = CodeDomProvider.CreateProvider ("C#");
			var result = provider.CompileAssemblyFromFile (compilerOptions, sourceFiles.ToArray ());
			if (!result.Errors.HasErrors)
				return compilerOptions.OutputAssembly.ToNPath ();

			var errors = new StringBuilder ();
			foreach (var error in result.Errors)
				errors.AppendLine (error.ToString ());
			throw new Exception ("Compilation errors: " + errors);
		}

		private IEnumerable<NPath> CompileBeforeTestAssemblies (NPath outputDirectory, string[] references, string[] defines)
		{
			foreach (var compileRefInfo in _unityMetadataProvider.GetCompileAssembliesBefore ())
			{
				var sandboxSourceDir = _sandbox.BeforeReferenceSourceDirectoryFor (compileRefInfo.OutputName);
				var sourceFiles = sandboxSourceDir.Files ("*.cs").Select(f => f.ToString ()).ToArray ();
				var output = CompileSupportingAssembly (compileRefInfo, outputDirectory, sourceFiles, references, defines);
				if (compileRefInfo.AddAsReference)
					yield return output;
			}
		}

		private void CompileAfterTestAssemblies (NPath outputDirectory, string[] references, string[] defines)
		{
			foreach (var compileRefInfo in _unityMetadataProvider.GetCompileAssembliesAfter ())
			{
				var sandboxSourceDir = _sandbox.AfterReferenceSourceDirectoryFor (compileRefInfo.OutputName);
				var sourceFiles = sandboxSourceDir.Files ("*.cs").Select (f => f.ToString ()).ToArray ();
				CompileSupportingAssembly (compileRefInfo, outputDirectory, sourceFiles, references, defines);
			}
		}

		protected virtual NPath CompileSupportingAssembly (CompileAssemblyInfo compileAssemblyInfo, NPath outputDirectory, string[] sourceFiles, string[] references, string[] defines)
		{
			var allDefines = defines.Concat (compileAssemblyInfo.Defines ?? new string[] {}).ToArray ();
			var allReferences = references.Concat (compileAssemblyInfo.References?.Select (p => MakeSupportingAssemblyReferencePathAbsolute (outputDirectory, p)) ?? new string[] { }).ToArray ();
			return CompileAssembly (outputDirectory.Combine (compileAssemblyInfo.OutputName), sourceFiles, allReferences, allDefines);
		}

		protected virtual string MakeSupportingAssemblyReferencePathAbsolute (NPath outputDirectory, string referenceFileName)
		{
			// Not a good idea to use a full path in a test, but maybe someone is trying to quickly test something locally
			if (Path.IsPathRooted (referenceFileName))
				return referenceFileName;

			var possiblePath = outputDirectory.Combine (referenceFileName);
			if (possiblePath.FileExists ())
				return possiblePath.ToString ();

			return referenceFileName;
		}

		protected virtual NPath CompileAssembly (NPath outputPath, string[] sourceFiles, string[] references, string[] defines)
		{
			var compilerOptions = CreateCompilerOptions (outputPath, references, defines);
			var provider = CodeDomProvider.CreateProvider ("C#");
			var result = provider.CompileAssemblyFromFile (compilerOptions, sourceFiles.ToArray ());
			if (!result.Errors.HasErrors)
				return compilerOptions.OutputAssembly.ToNPath ();

			var errors = new StringBuilder ();
			foreach (var error in result.Errors)
				errors.AppendLine(error.ToString ());
			throw new Exception ("Compilation errors: " + errors);
		}

		protected virtual CompilerParameters CreateCompilerOptions (NPath outputPath, IEnumerable<string> references, IEnumerable<string> defines)
		{
			var compilerParameters = new CompilerParameters
			{
				OutputAssembly = outputPath.ToString (),
				GenerateExecutable = outputPath.FileName.EndsWith (".exe")
			};

			compilerParameters.CompilerOptions = defines?.Aggregate (string.Empty, (buff, arg) => $"{buff} /define:{arg}");

			compilerParameters.ReferencedAssemblies.AddRange (references.ToArray ());

			return compilerParameters;
		}
	}
}