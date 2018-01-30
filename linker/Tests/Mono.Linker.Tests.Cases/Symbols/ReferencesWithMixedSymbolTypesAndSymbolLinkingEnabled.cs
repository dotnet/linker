﻿using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

[assembly: KeptAttributeAttribute (typeof (System.Diagnostics.DebuggableAttribute))]

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Will fix in follow on PR.  Fails due to all problems")]
	[Reference ("LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll", "input/LibraryWithMdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithMdb/LibraryWithMdb.dll.mdb", "input/LibraryWithMdb.dll.mdb")]

	[Reference ("LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll", "input/LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb", "input/LibraryWithPdb.pdb")]

	[SetupCompileBefore ("LibraryWithCompilerDefaultSymbols.dll", new[] { "Dependencies/LibraryWithCompilerDefaultSymbols.cs" }, additionalArguments: "/debug:full")]
	[SetupCompileBefore ("LibraryWithPortablePdbSymbols.dll", new[] { "Dependencies/LibraryWithPortablePdbSymbols.cs" }, additionalArguments: "/debug:portable", compilerToUse: "csc")]
	[SetupCompileBefore ("LibraryWithEmbeddedPdbSymbols.dll", new[] { "Dependencies/LibraryWithEmbeddedPdbSymbols.cs" }, additionalArguments: "/debug:embedded", compilerToUse: "csc")]

	[SetupCompileArgument ("/debug:full")]
	[SetupLinkerLinkSymbols ("true")]

	[KeptSymbols ("test.exe")]
	[KeptSymbols ("LibraryWithMdb.dll")]
#if WIN32
	[KeptSymbols ("LibraryWithPdb.dll")]
#else
	[RemovedSymbols ("LibraryWithPdb.dll")]
#endif
	[KeptSymbols ("LibraryWithCompilerDefaultSymbols.dll")]
	[KeptSymbols ("LibraryWithEmbeddedPdbSymbols.dll")]
	[KeptSymbols ("LibraryWithPortablePdbSymbols.dll")]


	[KeptMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithMdb.dll", typeof (LibraryWithMdb), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithCompilerDefaultSymbols.dll", typeof (LibraryWithCompilerDefaultSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithCompilerDefaultSymbols.dll", typeof (LibraryWithCompilerDefaultSymbols), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithEmbeddedPdbSymbols.dll", typeof (LibraryWithEmbeddedPdbSymbols), "NotUsed()")]

	[KeptMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPortablePdbSymbols.dll", typeof (LibraryWithPortablePdbSymbols), "NotUsed()")]
	public class ReferencesWithMixedSymbolTypesAndSymbolLinkingEnabled {
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod ();
			LibraryWithCompilerDefaultSymbols.SomeMethod ();
			LibraryWithPdb.SomeMethod ();
			LibraryWithMdb.SomeMethod ();
			LibraryWithEmbeddedPdbSymbols.SomeMethod ();
			LibraryWithPortablePdbSymbols.SomeMethod ();
		}

		[Kept]
		static void SomeMethod ()
		{
		}

		static void NotUsed ()
		{
		}
	}
}
