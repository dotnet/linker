﻿using System;
using System.Threading.Tasks;
using Xunit;

namespace ILLink.RoslynAnalyzer.Tests.Inheritance.Interfaces
{
	public sealed partial class StaticInterfaceMethodsTests : LinkerTestBase
	{

		protected override string TestSuiteName => "Inheritance.Interfaces.StaticInterfaceMethods";

		[Fact]
		public Task StaticAbstractInterfaceMethods ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticAbstractInterfaceMethodsLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

	}
}