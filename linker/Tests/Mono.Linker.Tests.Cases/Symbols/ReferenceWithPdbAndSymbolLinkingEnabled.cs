﻿using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Symbols.Dependencies;

namespace Mono.Linker.Tests.Cases.Symbols {
	[IgnoreTestCase ("Will fix in follow on PR.  Fails on OSX due to trying to save a native pdb")]
	[Reference ("LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.dll", "input/LibraryWithPdb.dll")]
	[SandboxDependency ("Dependencies/LibraryWithPdb/LibraryWithPdb.pdb", "input/LibraryWithPdb.pdb")]
	[SetupLinkerLinkSymbols( "true")]

#if WIN32
	[KeptSymbols ("LibraryWithPdb.dll")]
#else
	[RemovedSymbols ("LibraryWithPdb.dll")]
#endif
	[KeptMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "SomeMethod()")]
	[RemovedMemberInAssembly ("LibraryWithPdb.dll", typeof (LibraryWithPdb), "NotUsed()")]
	class ReferenceWithPdbAndSymbolLinkingEnabled {
		static void Main ()
		{
			// Use some stuff so that we can verify that the linker output correct results
			SomeMethod();
			LibraryWithPdb.SomeMethod ();
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
