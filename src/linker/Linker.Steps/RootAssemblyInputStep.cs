// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class RootAssemblyInput : BaseStep
	{
		readonly string fileName;

		public RootAssemblyInput (string fileName)
		{
			this.fileName = fileName;
		}

		protected override void Process ()
		{
			AssemblyDefinition assembly = LoadAssemblyFile ();

			var di = new DependencyInfo (DependencyKind.RootAssembly, assembly);

			AssemblyAction action = Context.Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.CopyUsed:
				Annotations.Mark (assembly.MainModule, di);
				goto case AssemblyAction.Copy;
			case AssemblyAction.Copy:
				// Mark Step wil take care of marking whole assembly
				return;
			case AssemblyAction.Link:
				break;
			default:
				Context.LogError ($"Root assembly '{assembly.Name}' cannot use action '{action}'", 1035);
				return;
			}

			switch (Context.GetAssemblyRootsMode (assembly.Name)) {
			case AssemblyRootsMode.Default:
				if (assembly.MainModule.Kind == ModuleKind.Dll)
					goto case AssemblyRootsMode.AllMembers;
				else
					goto case AssemblyRootsMode.EntryPoint;
			case AssemblyRootsMode.EntryPoint:
				var ep = assembly.MainModule.EntryPoint;
				if (ep == null) {
					Context.LogError ($"Root assembly '{assembly.Name}' does not have entry point", 1034);
					return;
				}

				Annotations.Mark (ep.DeclaringType.Module, di);
				Annotations.Mark (ep.DeclaringType, di);
				Annotations.AddPreservedMethod (ep.DeclaringType, ep);
				break;
			case AssemblyRootsMode.VisibleMembers:
				TypePreserve preserve = TypePreserve.All |
					(HasInternalsVisibleTo (assembly) ? TypePreserve.AccessibilityVisibleOrInternal : TypePreserve.AccessibilityVisible);

				foreach (var type in assembly.MainModule.Types)
					MarkAndPreserveVisible (type, preserve);
				break;
			case AssemblyRootsMode.AllMembers:
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

			Context.Resolver.CacheAssembly (assembly);
			Context.RegisterAssembly (assembly);
			return assembly;
		}

		void MarkAndPreserveVisible (TypeDefinition type, TypePreserve preserve)
		{
			switch (preserve & TypePreserve.AccessibilityMask) {
			case TypePreserve.AccessibilityVisible when !IsTypeVisible (type):
				return;
			case TypePreserve.AccessibilityVisibleOrInternal when !IsTypeVisibleOrInternal (type):
				return;
			}

			Annotations.Mark (type, new DependencyInfo (DependencyKind.RootAssembly, type.Module.Assembly));
			Annotations.SetPreserve (type, preserve);

			if (!type.HasNestedTypes)
				return;

			foreach (TypeDefinition nested in type.NestedTypes)
				MarkAndPreserveVisible (nested, preserve);
		}

		static bool IsTypeVisible (TypeDefinition type)
		{
			return type.IsPublic || type.IsNestedPublic || type.IsNestedFamily || type.IsNestedFamilyOrAssembly;
		}

		static bool IsTypeVisibleOrInternal (TypeDefinition type)
		{
			return !type.IsNestedPrivate;
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
