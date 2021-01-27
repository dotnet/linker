// Licensed to the.NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class MarkExportedTypesTargetStep : BasePerAssemblyStep
	{
		public MarkExportedTypesTargetStep (AssemblyDefinition assembly, LinkContext context) : base (assembly, context)
		{
		}

		public override void Process ()
		{
			if (!_assembly.MainModule.HasExportedTypes)
				return;

			foreach (var type in _assembly.MainModule.ExportedTypes)
				InitializeExportedType (type);
		}

		void InitializeExportedType (ExportedType exportedType)
		{
			if (!Annotations.IsMarked (exportedType))
				return;

			if (!Annotations.TryGetPreservedMembers (exportedType, out TypePreserveMembers members))
				return;

			TypeDefinition type = exportedType.Resolve ();
			if (type == null) {
				if (!Context.IgnoreUnresolved)
					Context.LogError ($"Exported type '{type.Name}' cannot be resolved", 1038);

				return;
			}

			Context.Annotations.Mark (type, new DependencyInfo (DependencyKind.ExportedType, exportedType));
			Annotations.SetMembersPreserve (type, members);
		}
	}
}