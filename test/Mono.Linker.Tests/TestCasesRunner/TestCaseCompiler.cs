﻿﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Extensions;
using NUnit.Framework;
#if NETCOREAPP
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;
using Microsoft.CodeAnalysis.CSharp;
#endif

namespace Mono.Linker.Tests.TestCasesRunner {
	public class TestCaseCompiler {
		static string _cachedWindowsCscPath = null;
		protected readonly TestCaseMetadaProvider _metadataProvider;
		protected readonly TestCaseSandbox _sandbox;
		protected readonly ILCompiler _ilCompiler;

		public TestCaseCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider)
			: this(sandbox, metadataProvider, new ILCompiler ())
		{
		}

		public TestCaseCompiler (TestCaseSandbox sandbox, TestCaseMetadaProvider metadataProvider, ILCompiler ilCompiler)
		{
			_ilCompiler = ilCompiler;
			_sandbox = sandbox;
			_metadataProvider = metadataProvider;
		}

		public NPath CompileTestIn (NPath outputDirectory, string outputName, IEnumerable<string> sourceFiles, string[] commonReferences, string[] mainAssemblyReferences, IEnumerable<string> defines, NPath[] resources, string[] additionalArguments)
		{
			var originalCommonReferences = commonReferences.Select (r => r.ToNPath ()).ToArray ();
			var originalDefines = defines?.ToArray () ?? new string [0];

			Prepare (outputDirectory);

			var compiledReferences = CompileBeforeTestCaseAssemblies (outputDirectory, originalCommonReferences, originalDefines).ToArray ();
			var allTestCaseReferences = originalCommonReferences
				.Concat (compiledReferences)
				.Concat (mainAssemblyReferences.Select (r => r.ToNPath ()))
				.ToArray ();

			var options = CreateOptionsForTestCase (
				outputDirectory.Combine (outputName),
				sourceFiles.Select (s => s.ToNPath ()).ToArray (),
				allTestCaseReferences,
				originalDefines,
				resources,
				additionalArguments);
			var testAssembly = CompileAssembly (options);
				

			// The compile after step is used by tests to mess around with the input to the linker.  Generally speaking, it doesn't seem like we would ever want to mess with the
			// expectations assemblies because this would undermine our ability to inspect them for expected results during ResultChecking.  The UnityLinker UnresolvedHandling tests depend on this
			// behavior of skipping the after test compile
			if (outputDirectory != _sandbox.ExpectationsDirectory)
				CompileAfterTestCaseAssemblies (outputDirectory, originalCommonReferences, originalDefines);

			return testAssembly;
		}

		protected virtual void Prepare (NPath outputDirectory)
		{
		}

		protected virtual CompilerOptions CreateOptionsForTestCase (NPath outputPath, NPath[] sourceFiles, NPath[] references, string[] defines, NPath[] resources, string[] additionalArguments)
		{
			return new CompilerOptions
			{
				OutputPath = outputPath,
				SourceFiles = sourceFiles,
				References = references,
				Defines = defines.Concat (_metadataProvider.GetDefines ()).ToArray (),
				Resources = resources,
				AdditionalArguments = additionalArguments,
				CompilerToUse = _metadataProvider.GetCSharpCompilerToUse ()
			};
		}

		protected virtual CompilerOptions CreateOptionsForSupportingAssembly (SetupCompileInfo setupCompileInfo, NPath outputDirectory, NPath[] sourceFiles, NPath[] references, string[] defines, NPath[] resources)
		{
			var allDefines = defines.Concat (setupCompileInfo.Defines ?? new string [0]).ToArray ();
			var allReferences = references.Concat (setupCompileInfo.References?.Select (p => MakeSupportingAssemblyReferencePathAbsolute (outputDirectory, p)) ?? new NPath [0]).ToArray ();
			string[] additionalArguments = string.IsNullOrEmpty (setupCompileInfo.AdditionalArguments) ? null : new[] { setupCompileInfo.AdditionalArguments };
			return new CompilerOptions
			{
				OutputPath = outputDirectory.Combine (setupCompileInfo.OutputName),
				SourceFiles = sourceFiles,
				References = allReferences,
				Defines = allDefines,
				Resources = resources,
				AdditionalArguments = additionalArguments,
				CompilerToUse = setupCompileInfo.CompilerToUse?.ToLower ()
			};
		}

		private IEnumerable<NPath> CompileBeforeTestCaseAssemblies (NPath outputDirectory, NPath[] references, string[] defines)
		{
			foreach (var setupCompileInfo in _metadataProvider.GetSetupCompileAssembliesBefore ())
			{
				var options = CreateOptionsForSupportingAssembly (
					setupCompileInfo,
					outputDirectory,
					CollectSetupBeforeSourcesFiles (setupCompileInfo),
					references,
					defines,
					CollectSetupBeforeResourcesFiles (setupCompileInfo));
				var output = CompileAssembly (options);
				if (setupCompileInfo.AddAsReference)
					yield return output;
			}
		}

		private void CompileAfterTestCaseAssemblies (NPath outputDirectory, NPath[] references, string[] defines)
		{
			foreach (var setupCompileInfo in _metadataProvider.GetSetupCompileAssembliesAfter ())
			{
				var options = CreateOptionsForSupportingAssembly (
					setupCompileInfo,
					outputDirectory,
					CollectSetupAfterSourcesFiles (setupCompileInfo),
					references,
					defines,
					CollectSetupAfterResourcesFiles (setupCompileInfo));
				CompileAssembly (options);
			}
		}

		private NPath[] CollectSetupBeforeSourcesFiles (SetupCompileInfo info)
		{
			return CollectSourceFilesFrom (_sandbox.BeforeReferenceSourceDirectoryFor (info.OutputName));
		}

		private NPath[] CollectSetupAfterSourcesFiles (SetupCompileInfo info)
		{
			return CollectSourceFilesFrom (_sandbox.AfterReferenceSourceDirectoryFor (info.OutputName));
		}
		
		private NPath[] CollectSetupBeforeResourcesFiles (SetupCompileInfo info)
		{
			return _sandbox.BeforeReferenceResourceDirectoryFor (info.OutputName).Files ().ToArray ();
		}

		private NPath[] CollectSetupAfterResourcesFiles (SetupCompileInfo info)
		{
			return  _sandbox.AfterReferenceResourceDirectoryFor (info.OutputName).Files ().ToArray ();
		}

		private static NPath[] CollectSourceFilesFrom (NPath directory)
		{
			var sourceFiles = directory.Files ("*.cs").ToArray ();
			if (sourceFiles.Length > 0)
				return sourceFiles;

			sourceFiles = directory.Files ("*.il").ToArray ();
			if (sourceFiles.Length > 0)
				return sourceFiles;

			throw new FileNotFoundException ($"Didn't find any sources files in {directory}");
		}

		protected static NPath MakeSupportingAssemblyReferencePathAbsolute (NPath outputDirectory, string referenceFileName)
		{
			// Not a good idea to use a full path in a test, but maybe someone is trying to quickly test something locally
			if (Path.IsPathRooted (referenceFileName))
				return referenceFileName.ToNPath ();

			var possiblePath = outputDirectory.Combine (referenceFileName);
			if (possiblePath.FileExists ())
				return possiblePath;

			return referenceFileName.ToNPath();
		}

		protected NPath CompileAssembly (CompilerOptions options)
		{
			if (options.SourceFiles.Any (path => path.ExtensionWithDot == ".cs"))
				return CompileCSharpAssembly (options);

			if (options.SourceFiles.Any (path => path.ExtensionWithDot == ".il"))
				return CompileIlAssembly (options);

			throw new NotSupportedException ($"Unable to compile sources files with extension `{options.SourceFiles.First ().ExtensionWithDot}`");
		}

		protected virtual NPath CompileCSharpAssemblyWithDefaultCompiler (CompilerOptions options)
		{
#if NETCOREAPP
			return CompileCSharpAssemblyWithRoslyn (options);
#else
			return CompileCSharpAssemblyWithCsc (options);
#endif
		}

#if NETCOREAPP
		protected virtual NPath CompileCSharpAssemblyWithRoslyn (CompilerOptions options)
		{
			var languageVersion = LanguageVersion.Default;
			var compilationOptions = new CSharpCompilationOptions (
				outputKind: options.OutputPath.FileName.EndsWith (".exe") ? OutputKind.ConsoleApplication : OutputKind.DynamicallyLinkedLibrary,
				assemblyIdentityComparer: DesktopAssemblyIdentityComparer.Default
			);
			// Default debug info format for the current platform.
			DebugInformationFormat debugType = RuntimeInformation.IsOSPlatform (OSPlatform.Windows) ? DebugInformationFormat.Pdb : DebugInformationFormat.PortablePdb;
			bool emitPdb = false;
			if (options.AdditionalArguments != null) {
				foreach (var option in options.AdditionalArguments) {
					switch (option) {
						case "/unsafe":
							compilationOptions = compilationOptions.WithAllowUnsafe(true);
							break;
						case "/optimize+":
							compilationOptions = compilationOptions.WithOptimizationLevel(OptimizationLevel.Release);
							break;
						case "/debug:full":
						case "/debug:pdbonly":
							// Use platform's default debug info. This behavior is the same as csc.
							emitPdb = true;
							break;
						case "/debug:portable":
							emitPdb = true;
							debugType = DebugInformationFormat.PortablePdb;
							break;
						case "/debug:embedded":
							emitPdb = true;
							debugType = DebugInformationFormat.Embedded;
							break;
						case "/langversion:7.3":
							languageVersion = LanguageVersion.CSharp7_3;
							break;
							
					}
				}
			}
			var parseOptions = new CSharpParseOptions (preprocessorSymbols: options.Defines, languageVersion: languageVersion);
			var emitOptions = new EmitOptions (debugInformationFormat: debugType);
			var pdbPath = (!emitPdb || debugType == DebugInformationFormat.Embedded) ? null : options.OutputPath.ChangeExtension (".pdb").ToString ();

			var syntaxTrees = options.SourceFiles.Select (p =>
				CSharpSyntaxTree.ParseText (
					text: p.ReadAllText (),
					options: parseOptions
				)
			);

			var compilation = CSharpCompilation.Create (
				assemblyName: options.OutputPath.FileNameWithoutExtension,
				syntaxTrees: syntaxTrees,
				references: options.References.Select (r => MetadataReference.CreateFromFile (r)),
				options: compilationOptions
			);

			var manifestResources = options.Resources.Select (r => {
				var fullPath = r.ToString ();
				return new ResourceDescription (
					resourceName: Path.GetFileName (fullPath),
					dataProvider: () => new FileStream (fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite),
					isPublic: true
				);
			});

			EmitResult result;
			using (var outputStream = File.Create (options.OutputPath.ToString ()))
			using (var pdbStream = (pdbPath == null ? null : File.Create (pdbPath))) {
				result = compilation.Emit(
					peStream: outputStream,
					pdbStream: pdbStream,
					manifestResources: manifestResources,
					options: emitOptions
				);
			}

			var errors = new StringBuilder ();
			if (result.Success)
				return options.OutputPath;

			foreach (var diagnostic in result.Diagnostics)
				errors.AppendLine (diagnostic.ToString ());
			throw new Exception ("Roslyn compilation errors: " + errors);
		}
#endif

		protected virtual NPath CompileCSharpAssemblyWithCsc (CompilerOptions options)
		{
#if NETCOREAPP
			return CompileCSharpAssemblyWithRoslyn (options);
#else
			return CompileCSharpAssemblyWithExternalCompiler (LocateCscExecutable (), options, "/shared ");
#endif
		}

		protected virtual NPath CompileCSharpAssemblyWithMcs(CompilerOptions options)
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				CompileCSharpAssemblyWithExternalCompiler (LocateMcsExecutable (), options, string.Empty);

			return CompileCSharpAssemblyWithDefaultCompiler (options);
		}

		protected NPath CompileCSharpAssemblyWithExternalCompiler (string executable, CompilerOptions options, string compilerSpecificArguments)
		{
			var capturedOutput = new List<string> ();
			var process = new Process ();
			process.StartInfo.FileName = executable;
			process.StartInfo.Arguments = OptionsToCompilerCommandLineArguments (options, compilerSpecificArguments);
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0)
				Assert.Fail ($"Failed to compile assembly with csc: {options.OutputPath}\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");

			return options.OutputPath;
		}

		static string LocateCscExecutable ()
		{
			if (Environment.OSVersion.Platform != PlatformID.Win32NT)
				return "csc";

			if (_cachedWindowsCscPath != null)
				return _cachedWindowsCscPath;

			var capturedOutput = new List<string> ();
			var process = new Process ();

			var vswherePath = Environment.ExpandEnvironmentVariables ("%ProgramFiles(x86)%\\Microsoft Visual Studio\\Installer\\vswhere.exe");
			if (!File.Exists (vswherePath))
				Assert.Fail ($"Unable to locate csc.exe on windows because vshwere.exe was not found at {vswherePath}");

			process.StartInfo.FileName = vswherePath;
			process.StartInfo.Arguments = "-latest -products * -requires Microsoft.Component.MSBuild -property installationPath";
			process.StartInfo.UseShellExecute = false;
			process.StartInfo.CreateNoWindow = true;
			process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
			process.StartInfo.RedirectStandardOutput = true;
			process.OutputDataReceived += (sender, args) => capturedOutput.Add (args.Data);
			process.Start ();
			process.BeginOutputReadLine ();
			process.WaitForExit ();

			if (process.ExitCode != 0)
				Assert.Fail ($"vswhere.exe failed with :\n{capturedOutput.Aggregate ((buff, s) => buff + Environment.NewLine + s)}");

			if (capturedOutput.Count == 0 || string.IsNullOrEmpty (capturedOutput [0]))
				Assert.Fail ("vswhere.exe was unable to locate an install directory");

			var installPath = capturedOutput [0].Trim ().ToNPath ();

			if (!installPath.Exists ())
				Assert.Fail ($"No install found at {installPath}");

			// Do a search for the roslyn directory for a little bit of future proofing since it normally lives under
			// a versioned msbuild directory
			foreach (var roslynDirectory in installPath.Directories ("Roslyn", true)) {
				var possibleCscPath = roslynDirectory.Combine ("csc.exe");
				if (possibleCscPath.Exists ()) {
					_cachedWindowsCscPath = possibleCscPath.ToString ();
					return _cachedWindowsCscPath;
				}
			}

			Assert.Fail ("Unable to locate a roslyn csc.exe");
			return null;
		}

		static string LocateMcsExecutable ()
		{
			if (Environment.OSVersion.Platform == PlatformID.Win32NT)
				Assert.Ignore ("We don't have a universal way of locating mcs on Windows");

			return "mcs";
		}

		protected string OptionsToCompilerCommandLineArguments (CompilerOptions options, string compilerSpecificArguments)
		{
			var builder = new StringBuilder ();
			if (!string.IsNullOrEmpty(compilerSpecificArguments))
				builder.Append (compilerSpecificArguments);
			builder.Append ($"/out:{options.OutputPath}");
			var target = options.OutputPath.ExtensionWithDot == ".exe" ? "exe" : "library";
			builder.Append ($" /target:{target}");
			if (options.Defines != null && options.Defines.Length > 0)
				builder.Append (options.Defines.Aggregate (string.Empty, (buff, arg) => $"{buff} /define:{arg}"));

			builder.Append (options.References.Aggregate (string.Empty, (buff, arg) => $"{buff} /r:{arg}"));

			if (options.Resources != null && options.Resources.Length > 0)
				builder.Append (options.Resources.Aggregate (string.Empty, (buff, arg) => $"{buff} /res:{arg}"));

			if (options.AdditionalArguments != null && options.AdditionalArguments.Length > 0)
				builder.Append (options.AdditionalArguments.Aggregate (string.Empty, (buff, arg) => $"{buff} {arg}"));

			builder.Append (options.SourceFiles.Aggregate (string.Empty, (buff, arg) => $"{buff} {arg}"));

			return builder.ToString ();
		}

		protected NPath CompileCSharpAssembly (CompilerOptions options)
		{
			if (string.IsNullOrEmpty (options.CompilerToUse))
				return CompileCSharpAssemblyWithDefaultCompiler (options);

			if (options.CompilerToUse == "csc")
				return CompileCSharpAssemblyWithCsc (options);

			if (options.CompilerToUse == "mcs")
				return CompileCSharpAssemblyWithMcs (options);

			throw new ArgumentException ($"Invalid compiler value `{options.CompilerToUse}`");
		}

		protected NPath CompileIlAssembly (CompilerOptions options)
		{
			return _ilCompiler.Compile (options);
		}
	}
}