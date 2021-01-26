using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class MarkStepContext : MarkContext
	{

		public readonly List<Action<AssemblyDefinition>> MarkAssemblyActions;
		public readonly List<Action<TypeDefinition>> MarkTypeActions;

		public MarkStepContext ()
		{
			MarkAssemblyActions = new List<Action<AssemblyDefinition>> ();
			MarkTypeActions = new List<Action<TypeDefinition>> ();
		}

		public override void RegisterMarkAssemblyAction (Action<AssemblyDefinition> action)
		{
			MarkAssemblyActions.Add (action);
		}

		public override void RegisterMarkTypeAction (Action<TypeDefinition> action)
		{
			MarkTypeActions.Add (action);
		}
	}
}