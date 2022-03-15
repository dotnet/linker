using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class InteropTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Interop";

		[Fact]
		public Task ComNullRefMauiRepro2 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}