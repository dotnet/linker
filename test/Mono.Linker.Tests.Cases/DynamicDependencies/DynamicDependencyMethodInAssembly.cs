using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", ".ctor()")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", "privateField")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary/Nested`1", "Method(T)")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/DynamicDependencyMethodInAssemblyLibrary.cs" })]
	public class DynamicDependencyMethodInAssembly
	{
		public static void Main ()
		{
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", "library")]
		[DynamicDependency (DynamicallyAccessedMemberTypes.NonPublicFields, "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary", "library")]
		[DynamicDependency ("Method(`0)", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.DynamicDependencyMethodInAssemblyLibrary.Nested`1", "library")]
		static void Dependency ()
		{
		}
	}
}
