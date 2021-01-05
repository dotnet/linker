// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Runtime.CompilerServices;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class TrimUsingVisibleMembers : BaseStep
	{
		readonly string fileName;

		public TrimUsingVisibleMembers (string fileName)
		{
			this.fileName = fileName;
		}

		protected override void Process ()
		{
			AssemblyDefinition assembly = TrimUsingEntryPoint.LoadAssemblyFile (fileName, Context);

			AssemblyAction action = Context.Annotations.GetAction (assembly);
			switch (action) {
			case AssemblyAction.CopyUsed:
				Annotations.Mark (assembly.MainModule, new DependencyInfo (DependencyKind.RootAssembly, assembly));
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

			TypePreserve preserve = TypePreserve.All |
				(Annotations.HasLinkerAttribute<InternalsVisibleToAttribute> (assembly) ? TypePreserve.AccessibilityVisibleOrInternal : TypePreserve.AccessibilityVisible);

			foreach (var type in assembly.MainModule.Types)
				MarkAndPreserveVisible (type, preserve);
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
	}
}
