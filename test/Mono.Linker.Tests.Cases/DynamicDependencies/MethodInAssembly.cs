using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInAssemblyLibrary", ".ctor()")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInAssemblyLibrary", "privateField")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MethodInAssemblyLibrary.cs" })]
	[SetupLinkerArgument ("--skip-unresolved", "true")]
	public class MethodInAssembly
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInAssemblyLibrary", "library")]
		[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicFields, "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInAssemblyLibrary", "library")]

		[ExpectedWarning ("IL2035", "NonExistentAssembly")]
		[DynamicDependency ("method", "type", "NonExistentAssembly")]
		static void Dependency ()
		{
		}
	}
}
