﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RootAssemblyInput : BaseStep
	{
		readonly string fileName;
		readonly AssemblyRootMode rootMode;

		public RootAssemblyInput (string fileName, AssemblyRootMode rootMode)
		{
			this.fileName = fileName;
			this.rootMode = rootMode;
		}

		protected override void Process ()
		{
			AssemblyDefinition assembly = LoadAssemblyFile ();

			var di = new DependencyInfo (DependencyKind.RootAssembly, assembly);

			AssemblyAction action = Context.Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.Copy:
				Annotations.Mark (assembly.MainModule, di);
				// Mark Step will take care of marking whole assembly
				return;
			case AssemblyAction.CopyUsed:
			case AssemblyAction.Link:
				break;
			default:
				Context.LogError ($"Root assembly '{assembly.Name}' cannot use action '{action}'", 1035);
				return;
			}

			switch (rootMode) {
			case AssemblyRootMode.Default:
				if (assembly.MainModule.Kind == ModuleKind.Dll)
					goto case AssemblyRootMode.AllMembers;
				else
					goto case AssemblyRootMode.EntryPoint;
			case AssemblyRootMode.EntryPoint:
				var ep = assembly.MainModule.EntryPoint;
				if (ep == null) {
					Context.LogError ($"Root assembly '{assembly.Name}' does not have entry point", 1034);
					return;
				}

				Annotations.Mark (ep.DeclaringType, di);
				Annotations.AddPreservedMethod (ep.DeclaringType, ep);
				break;
			case AssemblyRootMode.VisibleMembers:
				var preserve_visible = TypePreserveMembers.Visible;
				if (HasInternalsVisibleTo (assembly))
					preserve_visible |= TypePreserveMembers.Internal;

				MarkAndPreserve (assembly, preserve_visible);
				break;

			case AssemblyRootMode.Library:
				var preserve_library = TypePreserveMembers.Visible | TypePreserveMembers.Library;
				if (HasInternalsVisibleTo (assembly))
					preserve_library |= TypePreserveMembers.Internal;

				MarkAndPreserve (assembly, preserve_library);

				// Assembly root mode wins over any enabled optimization which
				// could conflict with library rooting behaviour
				Context.Optimizations.Disable (
					CodeOptimizations.Sealer |
					CodeOptimizations.UnusedTypeChecks |
					CodeOptimizations.UnreachableBodies |
					CodeOptimizations.UnusedInterfaces |
					CodeOptimizations.RemoveDescriptors |
					CodeOptimizations.RemoveLinkAttributes |
					CodeOptimizations.RemoveSubstitutions |
					CodeOptimizations.RemoveDynamicDependencyAttribute, assembly.Name.Name);
				break;
			case AssemblyRootMode.AllMembers:
				Context.Annotations.SetAction (assembly, AssemblyAction.Copy);
				return;
			}
		}

		AssemblyDefinition LoadAssemblyFile ()
		{
			AssemblyDefinition assembly = Context.Resolver.GetAssembly (fileName, Context.ReaderParameters);
			AssemblyDefinition loaded = Context.GetLoadedAssembly (assembly.Name.Name);

			// The same assembly could be already loaded if there are multiple inputs pointing to same file
			if (loaded != null)
				return loaded;

			Context.Resolver.CacheAssemblyWithPath (assembly);
			Context.RegisterAssembly (assembly);
			return assembly;
		}

		void MarkAndPreserve (AssemblyDefinition assembly, TypePreserveMembers preserve)
		{
			var module = assembly.MainModule;
			if (module.HasExportedTypes)
				foreach (var type in module.ExportedTypes)
					MarkAndPreserve (assembly, type, preserve);

			foreach (var type in module.Types)
				MarkAndPreserve (type, preserve);
		}

		void MarkAndPreserve (TypeDefinition type, TypePreserveMembers preserve)
		{
			TypePreserveMembers preserve_anything = preserve;
			if ((preserve & TypePreserveMembers.Visible) != 0 && !IsTypeVisible (type))
				preserve_anything &= ~TypePreserveMembers.Visible;

			if ((preserve & TypePreserveMembers.Internal) != 0 && IsTypePrivate (type))
				preserve_anything &= ~TypePreserveMembers.Internal;

			if (preserve_anything == 0)
				return;

			Annotations.Mark (type, new DependencyInfo (DependencyKind.RootAssembly, type.Module.Assembly));
			Annotations.SetMembersPreserve (type, preserve);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes)
				MarkAndPreserve (nested, preserve);
		}

		void MarkAndPreserve (AssemblyDefinition assembly, ExportedType type, TypePreserveMembers preserve)
		{
			var di = new DependencyInfo (DependencyKind.RootAssembly, assembly);
			Context.Annotations.Mark (type, di);
			Context.Annotations.Mark (assembly.MainModule, di);
			Annotations.SetMembersPreserve (type, preserve);
		}

		static bool IsTypeVisible (TypeDefinition type)
		{
			return type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly;
		}

		static bool IsTypePrivate (TypeDefinition type)
		{
			return type.IsNestedPrivate;
		}

		static bool HasInternalsVisibleTo (AssemblyDefinition assembly)
		{
			foreach (CustomAttribute attribute in assembly.CustomAttributes) {
				if (attribute.Constructor.DeclaringType.IsTypeOf ("System.Runtime.CompilerServices", "InternalsVisibleToAttribute"))
					return true;
			}

			return false;
		}
	}
}
