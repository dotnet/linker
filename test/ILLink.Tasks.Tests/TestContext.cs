using System;
using System.IO;
using System.Linq;
using Microsoft.DotNet.PlatformAbstractions;

namespace ILLink.Tests
{
	public static class TestContext
	{
		/// <summary>
		///   The root directory of the linker tasks. This
		///   contains a subdirectory for each
		///   TargetFramework, one for each build of the
		///   tasks.
		public static string TasksDirectoryRoot { get; private set; }

		/// <summary>
		///   The path to the dotnet tool to use to run the
		///   integration tests.
		/// </summary>
		public static string DotnetToolPath { get; set; }

		/// <summary>
		///   The RID to use when restoring, building, and linking the
		///   integration test projects.
		/// </summary>
		public static string RuntimeIdentifier { get; private set; }

		/// <summary>
		///   The configuration to use to build the integration test
		///   projects.
		/// </summary>
		public static string Configuration { get; private set; }

		/// <summary>
		///   The root testbin directory. This is where individual test
		///   projects are generated. Also used to install test
		///   assets that don't depend on the configuration or
		///   target framework.
		/// </summary>
		public static string TestBin { get; private set; }

		static TestContext()
		{
			SetupDefaultContext();
		}

		/// <summary>
		///   This is the context from which tests will be run in the
		///   linker repo. The local directory that contains the
		///   linker integration packages (hard-coded here) is
		///   searched for the tasks package. This assumes that only
		///   one version of the package is present, and uses it to
		///   unambiguously determine which pacakge to use in the tests.
		/// </summary>
		public static void SetupDefaultContext()
		{
			// Locate task dll
#if ARCADE
			// Tests are run from <root>/artifacts/bin/ILLink.Tasks.Tests/Release/netcoreapp3.0
			string repoRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", "..", ".."));
			TasksDirectoryRoot = Path.Combine(repoRoot, "artifacts", "bin", "ILLink.Tasks", "Release") + Path.DirectorySeparatorChar;
#else
			// Tests are run from <root>/bin/ILLink.Tasks.Tests/Debug/netcoreapp3.0
			string repoRoot = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, "..", "..", "..", ".."));
			TasksDirectoryRoot = Path.Combine(repoRoot, "bin", "ILLink.Tasks") + Path.DirectorySeparatorChar;
#endif
			if (!Directory.Exists(TasksDirectoryRoot))
				throw new Exception($"failed to locate local linker tasks at {TasksDirectoryRoot}");

			// Keep this in sync with TestBin defined in ILLink.Tasks.Tests.csproj
			TestBin = Path.Combine(repoRoot, "testprojects");

			// Locate dotnet host
			var dotnetDir = Path.Combine(repoRoot, ".dotnet");
			var dotnetToolName = Directory.GetFiles(dotnetDir)
				.Select(p => Path.GetFileName(p))
				.Where(p => p.StartsWith("dotnet"))
				.Where(p => {
					var ext = Path.GetExtension(p);
					return ext == "" || ext == ".exe";
				})
				.Single();
			DotnetToolPath = Path.Combine(dotnetDir, dotnetToolName);

			// This sets the RID to the RID of the currently-executing system.
			RuntimeIdentifier = RuntimeEnvironment.GetRuntimeIdentifier();
			// workaround: the osx.10.13-x64 RID doesn't exist yet.
			// see https://github.com/NuGet/Home/issues/5862
			if (RuntimeIdentifier == "osx.10.14-x64")
			{
				RuntimeIdentifier = "osx.10.13-x64";
			}
			// We want to build and link integration projects in the
			// release configuration.
			Configuration = "Release";
		}
	}
}
