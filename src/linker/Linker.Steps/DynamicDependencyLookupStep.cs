// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Mono.Cecil;

#nullable enable

namespace Mono.Linker.Steps
{
	public class DynamicDependencyLookupStep : BaseStep
	{
		protected override void ProcessAssembly (AssemblyDefinition assembly)
		{
			if (Annotations.GetAction (assembly) == AssemblyAction.Skip)
				return;

			var module = assembly.MainModule;

			foreach (var type in module.Types) {
				ProcessType (type);
			}
		}

		void ProcessType (TypeDefinition type)
		{
			if (type.HasMethods) {
				foreach (var method in type.GetMethods ()) {
					var methodDefinition = method.Resolve ();
					if (methodDefinition == null)
						continue;

					ProcessDynamicDependencyAttributes (methodDefinition);
				}
			}

			if (type.HasFields) {
				foreach (var field in type.Fields) {
					var fieldDefinition = field.Resolve ();
					if (fieldDefinition == null)
						continue;

					ProcessDynamicDependencyAttributes (fieldDefinition);
				}
			}

			if (type.HasNestedTypes) {
				foreach (var nestedType in type.NestedTypes) {
					ProcessType (nestedType);
				}
			}
		}

		void ProcessDynamicDependencyAttributes (IMemberDefinition member)
		{
			Debug.Assert (member is MethodDefinition || member is FieldDefinition);

			if (member.HasCustomAttributes) {
				foreach (var ca in member.CustomAttributes) {
					if (!IsPreserveDependencyAttribute (ca.AttributeType))
						continue;
#if FEATURE_ILLINK
					Context.LogWarning ($"'PreserveDependencyAttribute' is deprecated. Use 'DynamicDependencyAttribute' instead.", 2033, member);
#endif
					if (ca.ConstructorArguments.Count != 3)
						continue;

					if (!(ca.ConstructorArguments[2].Value is string assemblyName))
						continue;

					var assembly = Context.Resolve (new AssemblyNameReference (assemblyName, new Version ()));
					if (assembly == null)
						continue;
					Context.ProcessReferenceClosure (assembly);
				}
			}

			var dynamicDependencies = Context.Annotations.GetLinkerAttributes<DynamicDependency> (member);
			Debug.Assert (dynamicDependencies != null);

			foreach (var dynamicDependency in dynamicDependencies) {
				if (dynamicDependency.AssemblyName == null)
					continue;

				var assembly = Context.Resolve (new AssemblyNameReference (dynamicDependency.AssemblyName, new Version ()));
				if (assembly == null) {
					Context.LogWarning ($"Unresolved assembly '{dynamicDependency.AssemblyName}' in 'DynamicDependencyAttribute'", 2035, member);
					continue;
				}
				Context.ProcessReferenceClosure (assembly);
			}
		}

		public static bool IsPreserveDependencyAttribute (TypeReference tr)
		{
			return tr.Name == "PreserveDependencyAttribute" && tr.Namespace == "System.Runtime.CompilerServices";
		}
	}
}
