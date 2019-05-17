using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ILLink.Tests
{
	public class ConsoleFixture : TemplateProjectFixture
	{
		public ConsoleFixture (IMessageSink diagnosticMessageSink) : base (diagnosticMessageSink) {}

		protected override string TemplateName { get; } = "console";
	}

	public class ConsoleTest : IntegrationTestBase, IClassFixture<ConsoleFixture>
	{
		public ConsoleTest(ConsoleFixture fixture, ITestOutputHelper helper) : base(fixture, helper) {}

		[Fact]
		public void RunConsoleStandalone()
		{
			string executablePath = Link(Fixture.csproj, selfContained: true);
			CheckOutput(executablePath, selfContained: true);
		}

		[Fact]
		public void RunConsolePortable()
		{
			string target = Link(Fixture.csproj, selfContained: false);
			CheckOutput(target, selfContained: false);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			var ret = RunApp(target, selfContained: selfContained);
			Assert.True(ret.ExitCode == 0);
			Assert.Contains("Hello World!", ret.StdOut);
		}
	}
}
