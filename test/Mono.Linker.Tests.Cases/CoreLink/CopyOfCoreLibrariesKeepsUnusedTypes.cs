using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.CoreLink
{
	[SetupLinkerCoreAction ("copy")]

#if NETCOREAPP
	[KeptAssembly ("System.Private.CoreLib.dll")]
	[KeptAllTypesAndMembersInAssembly ("System.Private.CoreLib.dll")]
#else
	[KeptAssembly ("mscorlib.dll")]
	[KeptAllTypesAndMembersInAssembly ("mscorlib.dll")]
#endif

	// Can be removed once this bug is fixed https://bugzilla.xamarin.com/show_bug.cgi?id=58168
	[SkipPeVerify (SkipPeVerifyForToolchian.Pedump)]
	class CopyOfCoreLibrariesKeepsUnusedTypes
	{
		public static void Main()
		{
		}
	}
}
