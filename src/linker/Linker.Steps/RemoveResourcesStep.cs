using System;
using System.Linq;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RemoveResourcesStep : BaseStep 
	{
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (!assembly.MainModule.HasResources) return;
			RemoveFSharpCompilationResources (assembly);
		}

		private void RemoveFSharpCompilationResources (AssemblyDefinition assembly)
		{
			var resourcesInAssembly = assembly.MainModule.Resources.OfType<EmbeddedResource>();
			foreach (var resource in resourcesInAssembly.Where (IsFSharpCompilationResource)) {
				Annotations.AddResourceToRemove (assembly, resource);
			}

			static bool IsFSharpCompilationResource (Resource resource)
				=> resource.Name.StartsWith ("FSharpSignatureData", StringComparison.Ordinal)
				|| resource.Name.StartsWith ("FSharpOptimizationData", StringComparison.Ordinal);
		}
	}
}
