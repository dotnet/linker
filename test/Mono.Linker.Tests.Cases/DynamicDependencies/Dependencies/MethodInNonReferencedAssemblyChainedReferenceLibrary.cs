namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class MethodInNonReferencedAssemblyChainedReferenceLibrary : MethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			MethodInNonReferencedAssemblyChainedLibrary.Dependency ();
			return "Dependency";
		}
	}
}