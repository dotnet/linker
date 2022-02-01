using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Warnings
{
	public sealed partial class WarningSuppressionTests : LinkerTestBase
	{

		[Fact]
		public Task SuppressWarningsInAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SuppressWarningsInCopyAssembly ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SuppressWarningsUsingTargetViaXmlMono ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task SuppressWarningsUsingTargetViaXmlNetCore ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}