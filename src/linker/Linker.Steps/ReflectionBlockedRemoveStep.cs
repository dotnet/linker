using Mono.Cecil;

namespace Mono.Linker.Steps
{

	public class ReflectionBlockedRemoveStep : BaseStep
	{
		AssemblyDefinition assembly;

		protected override void ProcessAssembly(AssemblyDefinition assembly)
		{
			Annotations.CloseSymbolReader (assembly);
			Annotations.SetAction (assembly, AssemblyAction.Link);

			this.assembly = assembly;

			foreach (var type in assembly.MainModule.Types)
				ProcessType(type);
		}

		void ProcessType(TypeDefinition type)
		{
			foreach (var method in type.Methods)
				ProcessMethod(method);

			foreach (var nested in type.NestedTypes)
				ProcessType(nested);
		}

		void ProcessMethod(MethodDefinition method)
		{
			for (int i = 0; i < method.CustomAttributes.Count; i++) {
				var attr = method.CustomAttributes[i].AttributeType;
				if (attr.Namespace == "System.Runtime.CompilerServices" && attr.Name == "ReflectionBlockedAttribute")
					method.CustomAttributes.RemoveAt (i--);
			}
		}
	}
}
