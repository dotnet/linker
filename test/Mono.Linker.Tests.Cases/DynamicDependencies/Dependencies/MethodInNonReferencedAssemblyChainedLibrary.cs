using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class MethodInNonReferencedAssemblyChainedLibrary : MethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			Dependency ();
			return "Dependency";
		}

		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyBase2", "base2")]
		public static void Dependency ()
		{
		}
	}
}