using System;
using System.Diagnostics;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;

namespace ILLink.Tests
{

	public abstract class TemplateProjectFixture : ProjectFixture
	{
		public TemplateProjectFixture(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) {}

		/// <summary>
		///   The name of the project template to be passed to "dotnet new".
		/// </summary>
		protected abstract string TemplateName { get; }

		public override string SetupProject()
		{
			string projectRoot = Path.Combine(TestContext.TestBin, TemplateName);
			string csproj = Path.Combine(projectRoot, $"{TemplateName}.csproj");

			if (File.Exists(csproj)) {
				LogMessage ($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			var ret = CommandHelper.Dotnet($"new {TemplateName} --no-restore", projectRoot);
			if (ret.ExitCode != 0) {
				LogMessage ("dotnet new failed");
				Assert.True(false);
			}

			return csproj;

		}
	}

	/// <summary>
	///   Represents a project. Each fixture contains setup code run
	///   once before all tests in the same test class. ProjectFixture
	///   is the base type for different specific project
	///   fixtures. The surface area of this class represents the
	///   information each test class can access about the project
	///   under test.
	/// </summary>
	public abstract class ProjectFixture
	{
		private FixtureLogger logger;
		protected CommandHelper CommandHelper;

		public string csproj;

		/// <summary>
		///   Linker root files to be copied to be included in the
		///   test project directory. This may contain different files
		///   for standalone and portable publish.
		/// </summary>
		protected virtual HashSet<string> RootFiles { get; }

		public Dictionary<string, string> extraBuildArgs;

		protected void LogMessage (string message)
		{
			logger.LogMessage (message);
		}

		public ProjectFixture (IMessageSink diagnosticMessageSink)
		{
			logger = new FixtureLogger (diagnosticMessageSink);
			CommandHelper = new CommandHelper (logger);
			csproj = SetupProject();
			CopyRootFiles();
			// AddLinkerRoots();
			PrepareForLink();
		}

		public abstract string SetupProject();

		private void PrepareForLink()
		{
			Restore();
			Build(selfContained: true);
			Build(selfContained: false);
		}

		/// <summary>
		///   Copies linker root files from the integration test
		///   project to the individual test project.
		/// </summary>
		private void CopyRootFiles()
		{
			if (RootFiles == null)
				return;
			foreach (var rootFile in RootFiles) {
				if (!String.IsNullOrEmpty(rootFile))
					File.Copy(rootFile, Path.Combine(Path.GetDirectoryName(csproj), Path.GetFileName(rootFile)), overwrite: true);
			}
		}


		private void Restore()
		{
			string projectDir = Path.GetDirectoryName(csproj);
			string lockFile = Path.Combine(projectDir, "obj", "project.assets.json");
			if (File.Exists(lockFile))
			{
				LogMessage("using lock file at " + lockFile);
				return;
			}

			var restoreArgs = $"restore -r {TestContext.RuntimeIdentifier}";
			restoreArgs += $" /p:_ILLinkTasksDirectoryRoot={TestContext.TasksDirectoryRoot}";
			var ret = CommandHelper.Dotnet(restoreArgs, projectDir);
			if (ret.ExitCode != 0) {
				LogMessage("restore failed, returning " + ret);
				Assert.True(false);
			}
		}

		private void Build(bool selfContained)
		{
			string projectDir = Path.GetDirectoryName(csproj);

			// TODO: don't hard-code target framework
			string objPath = Path.Combine(projectDir, "obj", TestContext.Configuration, "netcoreapp3.0");
			string outputDllPath;
			if (selfContained) {
				outputDllPath = Path.Combine(objPath, TestContext.RuntimeIdentifier, Path.GetFileName(projectDir) + ".dll");
			} else {
				outputDllPath = Path.Combine(objPath, Path.GetFileName(projectDir) + ".dll");
			}
			if (File.Exists(outputDllPath)) {
				LogMessage("using build artifacts at " + outputDllPath);
				return;
			}

			string buildArgs = $"build --no-restore -c {TestContext.Configuration} /v:n";
			if (selfContained) {
				buildArgs += $" -r {TestContext.RuntimeIdentifier}";
			}
			if (extraBuildArgs != null) {
				foreach (var item in extraBuildArgs) {
					buildArgs += $" /p:{item.Key}={item.Value}";
				}
			}

			buildArgs += $" /p:_ILLinkTasksDirectoryRoot={TestContext.TasksDirectoryRoot}";
			var ret = CommandHelper.Dotnet(buildArgs, projectDir);

			if (ret.ExitCode != 0) {
				LogMessage("build failed, returning " + ret);
				Assert.True(false);
			}
		}
	}

	/// <summary>
	///   Contains logic shared by multiple test classes.
	/// </summary>
	public class IntegrationTestBase
	{
		private readonly TestLogger logger;
		protected readonly CommandHelper CommandHelper;
		protected readonly ProjectFixture Fixture;


		public IntegrationTestBase(ProjectFixture fixture, ITestOutputHelper output)
		{
			logger = new TestLogger(output);
			CommandHelper = new CommandHelper(logger);
			Fixture = fixture;
		}

		private void LogMessage (string message)
		{
			logger.LogMessage (message);
		}

		/// <summary>
		///   Run the linker on the specified project. This assumes
		///   that the project already contains a reference to the
		///   linker task package.
		///   Optionally takes a list of root descriptor files.
		///   Returns the path to the built app, either the renamed
		///   host for self-contained publish, or the dll containing
		///   the entry point.
		/// </summary>
		public string Link(string csproj, Dictionary<string, string> extraPublishArgs = null, bool selfContained = false, string rootFile = null)
		{
			string projectDir = Path.GetDirectoryName(csproj);

			string publishArgs = $"publish --no-build -c {TestContext.Configuration} /v:n /bl";
			if (selfContained) {
				publishArgs += $" -r {TestContext.RuntimeIdentifier}";
			}
			if (!String.IsNullOrEmpty (rootFile)) {
				publishArgs += $" /p:TrimmerRootDescriptors={rootFile}";
			}
			if (extraPublishArgs != null) {
				foreach (var item in extraPublishArgs) {
					publishArgs += $" /p:{item.Key}={item.Value}";
				}
			}

			publishArgs += $" /p:PublishTrimmed=true";
			// Use the local version of Microsoft.NET.ILLink.targets, by overriding
			// the properties set by the linker package's Sdk.props
			publishArgs += $" /p:_ILLinkTasksDirectoryRoot={TestContext.TasksDirectoryRoot}";

			// Output directory, relative to project
			string publishDir = "linked";
			if (!selfContained)
				publishDir += "_portable";
			publishArgs += $" -o {publishDir}";

			var ret = CommandHelper.Dotnet(publishArgs, projectDir);

			if (ret.ExitCode != 0) {
				LogMessage("publish failed, returning " + ret);
				Assert.True(false);
			}

			var builtApp = Path.Combine(projectDir, publishDir, Path.GetFileNameWithoutExtension(csproj));
			if (selfContained) {
				if (TestContext.RuntimeIdentifier.Contains("win"))
					builtApp += ".exe";
			} else {
				builtApp += ".dll";
			}
			Assert.True(File.Exists(builtApp));
			return builtApp;
		}

		public CommandResult RunApp(string target, int timeout = Int32.MaxValue,
			string terminatingOutput = null, bool selfContained = false)
		{
			Assert.True(File.Exists(target));
			if (selfContained) {
				return CommandHelper.RunCommand(
						target, null,
						Directory.GetParent(target).FullName,
						null, timeout, terminatingOutput);
			} else {
				return CommandHelper.RunCommand(
						Path.GetFullPath(TestContext.DotnetToolPath),
						Path.GetFullPath(target),
						Directory.GetParent(target).FullName,
						null, timeout, terminatingOutput);
			}
		}
	}
}
