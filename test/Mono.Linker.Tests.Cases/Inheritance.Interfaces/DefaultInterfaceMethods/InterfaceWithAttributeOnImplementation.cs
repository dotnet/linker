﻿using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.DefaultInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
#if !NETCOREAPP
	[IgnoreTestCase ("Only for .NET Core for some reason")]
#endif
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/InterfaceWithAttributeOnImpl.il" })]
	class InterfaceWithAttributeOnImplementation
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			((IMyInterface)new MyClass ()).Frob ();
#endif
		}
	}
}
