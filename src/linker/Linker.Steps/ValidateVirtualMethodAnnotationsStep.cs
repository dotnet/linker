// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ValidateVirtualMethodAnnotationsStep : BaseStep
	{
		protected override void Process ()
		{
			var annotations = Context.Annotations;
			foreach (var method in annotations.VirtualMethodsWithAnnotationsToValidate) {
				var baseMethods = annotations.GetBaseMethods (method);
				if (baseMethods != null) {
					foreach (var baseMethod in baseMethods) {
						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (method, baseMethod);
						ValidateMethodRequiresUnreferencedCodeAreSame (method, baseMethod);
					}
				}

				var overrides = annotations.GetOverrides (method);
				if (overrides != null) {
					foreach (var overrideInformation in overrides) {
						// Skip validation for cases where both base and override are in the list, we will validate the edge
						// when validating the override from the list.
						// This avoids validating the edge twice (it would produce the same warning twice)
						if (annotations.VirtualMethodsWithAnnotationsToValidate.Contains (overrideInformation.Override))
							continue;

						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (overrideInformation.Override, method);
						ValidateMethodRequiresUnreferencedCodeAreSame (overrideInformation.Override, method);
					}
				}
			}
		}

		void ValidateMethodRequiresUnreferencedCodeAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			var annotations = Context.Annotations;
			bool methodHasAttribute = annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method);
			if (methodHasAttribute != annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (baseMethod)) {
				string arg0 = nameof (RequiresUnreferencedCodeAttribute);
				string arg1 = method.GetDisplayName ();
				string arg2 = baseMethod.GetDisplayName ();
				if (!methodHasAttribute && !baseMethod.DeclaringType.IsInterface) {
					string message = string.Format (SharedStrings.BaseRequiresMismatchMessage, arg0, arg1, arg2);
					Context.LogWarning (message, 2046, method, subcategory: MessageSubCategory.TrimAnalysis);
				} else if (methodHasAttribute && !baseMethod.DeclaringType.IsInterface) {
					string message = string.Format (SharedStrings.DerivedRequiresMismatchMessage, arg0, arg1, arg2);
					Context.LogWarning (message, 2108, method, subcategory: MessageSubCategory.TrimAnalysis);
				}
				if (!methodHasAttribute && baseMethod.DeclaringType.IsInterface) {
					string message = string.Format (SharedStrings.InterfaceRequiresMismatchMessage, arg0, arg1, arg2);
					Context.LogWarning (message, 2109, method, subcategory: MessageSubCategory.TrimAnalysis);
				} else if (methodHasAttribute && baseMethod.DeclaringType.IsInterface) {
					string message = string.Format (SharedStrings.ImplementationRequiresMismatchMessage, arg0, arg1, arg2);
					Context.LogWarning (message, 2110, method, subcategory: MessageSubCategory.TrimAnalysis);
				}
			}
		}
	}
}
