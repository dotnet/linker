namespace Mono.Linker.Steps
{
    public class RemoveResourcesStep : IStep 
	{
        public void Process(LinkContext context)
        {
            var assemblies = context.Annotations.GetAssemblies().ToArray();

            foreach (var assembly in assemblies) {
                RemoveFSharpCompilationResources(assembly);
            }
        }

        private void RemoveFSharpCompilationResources(AssemblyDefinition assembly)
        {
            var resourcesInAssembly = assembly.MainModule.Resources.Select(r => r.Name);
            foreach (var resource in resourcesInAssembly.Where(IsFSharpCompilationResource)) {
                Annotations.AddResourceToRemove(assembly, resource);
            }

            static bool IsFSharpCompilationResource(string resourceName)
                => resourceName.StartsWith("FSharpSignatureData")
                || resourceName.StartsWith("FSharpOptimizationData");
        }
    }
}
