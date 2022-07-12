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

		[Fact]
		public Task StaticInterfaceMethodsInPreservedScope ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsInPreservedScope ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsInPreservedScopeLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task StaticVirtualInterfaceMethodsLibrary ()
		{
			return RunTest (allowMissingWarnings: true);
		}

		[Fact]
		public Task UnusedInterfacesInPreservedScope ()
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