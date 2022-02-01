﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests
{
	public sealed partial class AdvancedTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Advanced";

		[Fact]
		public Task DeadCodeElimination1 ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task FieldThatOnlyGetsSetIsRemoved ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeCheckRemoval ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task TypeCheckRemovalDisabled ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}