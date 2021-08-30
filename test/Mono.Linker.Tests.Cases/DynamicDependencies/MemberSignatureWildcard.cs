using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MethodInAssemblyLibrary.cs" })]
	[LogContains ("IL2037: Mono.Linker.Tests.Cases.DynamicDependencies.MemberSignatureWildcard.Dependency(): No members were resolved for '*'.")]
	public class MemberSignatureWildcard
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("*", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}
	}
}