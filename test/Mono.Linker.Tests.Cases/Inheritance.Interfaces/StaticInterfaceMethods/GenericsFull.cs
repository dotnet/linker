using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.StaticInterfaceMethods
{
	[SetupLinkerArgument ("--skip-unresolved", "true")]
#if !NETCOREAPP
	[IgnoreTestCase("Only for .NET Core for some reason")]
#endif
	[Define ("IL_ASSEMBLY_AVAILABLE")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/GenericsFull.il" })]
	[KeptTypeInAssembly ("library.dll", "IFaceNonGeneric")]
	[KeptTypeInAssembly ("library.dll", "IFaceGeneric`1")]
	[KeptTypeInAssembly ("library.dll", "IFaceCuriouslyRecurringGeneric`1")]

	class GenericsFull
	{
		static void Main ()
		{
#if IL_ASSEMBLY_AVAILABLE
			TestEntrypoint.Test();
#endif
		}
	}
}
