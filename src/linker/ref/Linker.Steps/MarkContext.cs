using System;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class MarkContext
	{
		public abstract void RegisterMarkAssemblyAction (Action<AssemblyDefinition> action);

		public abstract void RegisterMarkTypeAction (Action<TypeDefinition> action);

		public abstract void RegisterMarkMethodAction (Action<MethodDefinition> action);
	}
}