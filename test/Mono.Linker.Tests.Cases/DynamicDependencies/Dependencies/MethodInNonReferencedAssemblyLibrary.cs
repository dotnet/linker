namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
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