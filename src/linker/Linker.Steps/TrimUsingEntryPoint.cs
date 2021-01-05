// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class TrimUsingEntryPoint : BaseStep
	{
		readonly string fileName;

		public TrimUsingEntryPoint (string fileName)
		{
			this.fileName = fileName;
		}

		protected override void Process ()
		{
			AssemblyDefinition assembly = LoadAssemblyFile (fileName, Context);
			var ep = assembly.MainModule.EntryPoint;
			if (ep == null) {
				Context.LogError ($"Trimming assembly '{assembly.Name}' does not have entry point", 1034);
				return;
			}

			var di = new DependencyInfo (DependencyKind.RootAssembly, assembly);

			AssemblyAction action = Context.Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.CopyUsed:
				Annotations.Mark (ep.DeclaringType.Module, di);
				goto case AssemblyAction.Copy;
			case AssemblyAction.Copy:
				// Mark Step wil take care of marking whole assembly
				return;
			case AssemblyAction.Link:
				break;
			default:
				Context.LogError ($"Trimming assembly '{assembly.Name}' cannot use action '{action}'", 1035);
				return;
			}

			Annotations.Mark (ep.DeclaringType.Module, di);
			Annotations.Mark (ep.DeclaringType, di);
			Annotations.AddPreservedMethod (ep.DeclaringType, ep);
		}

		public static AssemblyDefinition LoadAssemblyFile (string fileName, LinkContext context)
		{
			AssemblyDefinition assembly = context.Resolver.GetAssembly (fileName, context.ReaderParameters);
			AssemblyDefinition loaded = context.GetLoadedAssembly (assembly.Name.Name);

			// The same assembly could be already loaded if there are multiple inputs pointing to same file
			if (loaded != null)
				return loaded;

			context.Resolver.CacheAssembly (assembly);
			context.RegisterAssembly (assembly);
			return assembly;
		}
	}
}
