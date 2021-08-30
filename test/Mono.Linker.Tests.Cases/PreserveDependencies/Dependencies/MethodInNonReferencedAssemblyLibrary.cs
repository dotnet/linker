namespace Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies
{
	public class MethodInNonReferencedAssemblyLibrary : MethodInNonReferencedAssemblyBase
	{
		public override string Method ()
		{
			return "Dependency";
		}

		private void UnusedMethod ()
		{
		}
	}
}