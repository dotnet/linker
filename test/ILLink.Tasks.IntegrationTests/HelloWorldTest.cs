using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ILLink.Tests
{
	public class HelloWorldFixture : ProjectFixture
	{
		public string csproj;

		public HelloWorldFixture (IMessageSink diagnosticMessageSink) : base (diagnosticMessageSink)
		{
			csproj = SetupProject ();
		}

		public string SetupProject()
		{
			string projectRoot = CreateTestFolder("helloworld");
			string csproj = Path.Combine(projectRoot, $"helloworld.csproj");

			if (File.Exists(csproj)) {
				LogMessage ($"using existing project {csproj}");
				return csproj;
			}

			if (Directory.Exists(projectRoot)) {
				Directory.Delete(projectRoot, true);
			}

			Directory.CreateDirectory(projectRoot);
			int ret = CommandHelper.Dotnet("new console", projectRoot);
			if (ret != 0) {
				LogMessage ("dotnet new failed");
				Assert.True(false);
			}

			AddLinkerReference(csproj);

			AddNuGetConfig(projectRoot);

			return csproj;
		}
	}

	public class HelloWorldTest : IntegrationTestBase, IClassFixture<HelloWorldFixture>
	{
		readonly HelloWorldFixture fixture;

		public HelloWorldTest(HelloWorldFixture fixture, ITestOutputHelper helper) : base(helper) {
			this.fixture = fixture;
		}

		[Fact]
		public void RunHelloWorldStandalone()
		{
			string executablePath = BuildAndLink(fixture.csproj, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			int ret = RunApp(target, out string commandOutput, selfContained: selfContained);
			Assert.True(ret == 0);
			Assert.Contains("Hello World!", commandOutput);
		}
	}
}
