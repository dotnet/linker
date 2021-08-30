namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
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