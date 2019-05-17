using System;
using System.IO;
using System.Xml.Linq;
using Xunit;
using Xunit.Abstractions;
using System.Collections.Generic;

namespace ILLink.Tests
{
	public class WebApiFixture : TemplateProjectFixture
	{
		public WebApiFixture(IMessageSink diagnosticMessageSink) : base(diagnosticMessageSink) {}

		protected override string TemplateName { get; } = "webapi";

		protected override HashSet<string> RootFiles { get; } = new HashSet<string> () { "WebApiReflection.xml", "WebApiReflectionPortable.xml" };

		// TODO: Remove this once we figure out what to do about apps
		// that have the publish output filtered by a manifest
		// file. It looks like aspnet has made this the default. See
		// the bug at https://github.com/dotnet/sdk/issues/1160.
		private void PreventPublishFiltering(string csproj) {
			var xdoc = XDocument.Load(csproj);
			var ns = xdoc.Root.GetDefaultNamespace();

			var propertygroup = xdoc.Root.Element(ns + "PropertyGroup");

			LogMessage("setting PublishWithAspNetCoreTargetManifest=false");
			propertygroup.Add(new XElement(ns + "PublishWithAspNetCoreTargetManifest",
										   "false"));

			using (var fs = new FileStream(csproj, FileMode.Create)) {
				xdoc.Save(fs);
			}
		}
	}

	public class WebApiTest : IntegrationTestBase, IClassFixture<WebApiFixture>
	{
		public WebApiTest(WebApiFixture fixture, ITestOutputHelper output) : base(fixture, output) {}

		[Fact]
		public void RunWebApiStandalone()
		{
			string executablePath = Link(Fixture.csproj, selfContained: true, rootFile: "WebApiReflection.xml");
			CheckOutput(executablePath, selfContained: true);
		}

		[Fact]
		public void RunWebApiPortable()
		{
			string target = Link(Fixture.csproj, selfContained: false, rootFile: "WebApiReflectionPortable.xml");
			CheckOutput(target, selfContained: false);
		}

		void CheckOutput(string target, bool selfContained = false)
		{
			string terminatingOutput = "Application started. Press Ctrl+C to shut down.";
			var ret = RunApp(target, 60000, terminatingOutput, selfContained: selfContained);
			Assert.Contains("Now listening on: http://localhost:5000", ret.StdOut);
			Assert.Contains(terminatingOutput, ret.StdOut);
		}
	}
}
