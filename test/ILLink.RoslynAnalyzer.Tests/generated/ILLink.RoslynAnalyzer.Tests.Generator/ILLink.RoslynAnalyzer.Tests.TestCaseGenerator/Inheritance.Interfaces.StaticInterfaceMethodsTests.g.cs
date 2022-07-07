using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.StaticInterfaceMethods";

		[Fact]
		public Task StaticVirtualInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfacesInPreserveScope ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedStaticInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}