// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;

namespace Mono.Linker.Steps
{
	public class RootNonTrimmableAssemblies : BaseStep
	{
		protected override void Process ()
		{
			// Walk over all -reference inputs and resolve any that may need to be rooted
			foreach (var assemblyPath in GetInputAssemblyPaths ()) {
				var assemblyName = Path.GetFileNameWithoutExtension (assemblyPath);

				if (!MaybeIsFullyPreservedAssembly (assemblyName))
					continue;

				var assembly = Context.TryResolve (assemblyName);
				if (assembly == null) {
					Context.LogError ($"Reference assembly '{assemblyPath}' could not be loaded", 1039);
					continue;
				}

				if (IsFullyPreservedAction (Annotations.GetAction (assembly)))
					Annotations.Mark (assembly.MainModule, new DependencyInfo (DependencyKind.AssemblyAction, assembly));
			}
		}

		public IEnumerable<string> GetInputAssemblyPaths ()
		{
			var assemblies = new HashSet<string> ();
			foreach (var referencePath in Context.Resolver.GetReferencePaths ()) {
				var assemblyName = Path.GetFileNameWithoutExtension (referencePath);
				if (assemblies.Add (assemblyName))
					yield return referencePath;
			}
		}

		public static bool IsFullyPreservedAction (AssemblyAction action)
		{
			return action == AssemblyAction.Copy || action == AssemblyAction.Save;
		}

		bool MaybeIsFullyPreservedAssembly (string assemblyName)
		{
			if (Context.Actions.TryGetValue (assemblyName, out AssemblyAction action))
				return IsFullyPreservedAction (action);

			return IsFullyPreservedAction (Context.DefaultAction) || IsFullyPreservedAction (Context.TrimAction);
		}
	}
}
