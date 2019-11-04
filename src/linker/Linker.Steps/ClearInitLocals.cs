using System;
using System.Collections.Generic;

using Mono.Linker;
using Mono.Linker.Steps;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ClearInitLocalsStep : BaseStep
	{
		HashSet<string> _assemblies;

		protected override void Process ()
		{
			string parameterName = "ClearInitLocalsAssemblies";

			if (Context.HasParameter (parameterName)) {
				string parameter = Context.GetParameter (parameterName);
				_assemblies = new HashSet<string> (parameter.Split(','), StringComparer.OrdinalIgnoreCase);
			}
		}

		private static List<TypeDefinition> GetAllTypesInModule (ModuleDefinition module)
		{
			List<TypeDefinition> allTypes = new List<TypeDefinition> (module.Types);

			// This is a breadth-first traversal through all the types in the assembly
			// at all levels of nesting. We use a 'for' loop instead of a 'foreach' loop
			// below because we're appending elements to the list while we iterate.

			for (int i = 0; i < allTypes.Count; i++) {
				allTypes.AddRange (allTypes [i].NestedTypes);
			}

			return allTypes;
		}

		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if ((_assemblies != null) && (!_assemblies.Contains (assembly.Name.Name))) {
				return;
			}

			bool changed = false;

			foreach (ModuleDefinition module in assembly.Modules) {
				foreach (TypeDefinition type in GetAllTypesInModule (module)) {
					foreach (MethodDefinition method in type.Methods) {
						if (method.Body != null) {
							if (method.Body.InitLocals) {
								method.Body.InitLocals = false;
								changed = true;
							}
						}
					}
				}
			}

			if (changed && (Annotations.GetAction (assembly) == AssemblyAction.Copy))
					Annotations.SetAction (assembly, AssemblyAction.Save);
		}
	}
}
