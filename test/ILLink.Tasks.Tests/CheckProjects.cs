using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ILLink.Tests
{
	[Collection("Console collection")]
	public class CheckProjects : IntegrationTestBase
	{
		public CheckProjects(ConsoleFixture fixture, ITestOutputHelper helper) : base(fixture, helper) {}

		[Fact]
		public void CheckTargetIncludes()
		{
			var projectRoot = Path.GetDirectoryName(Fixture.csproj);
			var ret = CommandHelper.Dotnet("msbuild /t:CheckTargetIncludes", projectRoot);
			Assert.True(ret.ExitCode == 0);
			Assert.Contains("test logic targets were included", ret.StdOut);
		}

		[Fact]
		public void CheckTasksPath()
		{
			var projectRoot = Path.GetDirectoryName(Fixture.csproj);
			var ret = CommandHelper.Dotnet("msbuild /t:CheckTasksPath", projectRoot);
			Assert.True(ret.ExitCode == 0);
			Assert.Contains($"_ILLinkTasksDirectoryRoot: {TestContext.TasksDirectoryRoot}", ret.StdOut);
		}
	}
}
