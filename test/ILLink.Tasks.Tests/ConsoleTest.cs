using System;
using System.IO;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace ILLink.Tests
{
	[CollectionDefinition("Console collection")]
	public class ConsoleCollection : ICollectionFixture<ConsoleFixture>
	{
		// This class exists to ensure that the console
		// project is shared between test classes that use it.
	}

	public class ConsoleFixture : TemplateProjectFixture
	{
		public ConsoleFixture (IMessageSink diagnosticMessageSink) : base (diagnosticMessageSink) {}

		protected override string TemplateName { get; } = "console";
	}

	[Collection("Console collection")]
	public class ConsoleTest : IntegrationTestBase
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
