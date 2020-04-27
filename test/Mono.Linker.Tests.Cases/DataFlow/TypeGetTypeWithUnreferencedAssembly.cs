using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DataFlow
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyReferencedWithTypeGetType.cs" }, addAsReference: false)]
	[SkipUnresolved(true)]
	[KeptAssembly ("library.dll")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DataFlow.TypeReferencedWithTypeGetType", "ReflectionReferencedMethod()")]
	class TypeGetTypeWithUnreferencedAssembly
	{
		public static void Main()
		{
			Type.GetType ("Mono.Linker.Tests.Cases.DataFlow.TypeReferencedWithTypeGetType, library")
				.GetMethod ("ReflectionReferencedMethod");
		}
	}
}
