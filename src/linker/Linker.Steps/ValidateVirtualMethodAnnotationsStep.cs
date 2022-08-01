// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using ILLink.Shared;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class ValidateVirtualMethodAnnotationsStep : BaseStep
	{
		protected override void Process ()
		{
			var annotations = Context.Annotations;
			foreach (var method in annotations.VirtualMethodsWithAnnotationsToValidate) {
				var overrides = annotations.GetOverrides (method);
				if (overrides != null) {
					foreach (var overrideInformation in overrides) {
						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (overrideInformation.Override, method);
						ValidateMethodRequiresUnreferencedCodeAreSame (overrideInformation.Override, method);
					}
				}

				// We look at every static virtual method, so we only need to search one way in the virtual/override chain
				//if (method.IsStatic)
				//	continue;

				var baseMethods = annotations.GetBaseMethods (method);
				if (baseMethods != null) {
					foreach (var baseMethod in baseMethods) {
						// Skip validation for cases where both base and override are in the list, we will validate the edge
						// when validating the override from the list.
						// This avoids validating the edge twice (it would produce the same warning twice)
						if (annotations.VirtualMethodsWithAnnotationsToValidate.Contains (baseMethod))
							continue;
						annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (method, baseMethod);
						ValidateMethodRequiresUnreferencedCodeAreSame (method, baseMethod);
					}
				}

			}
		}

		void ValidateMethodRequiresUnreferencedCodeAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			var annotations = Context.Annotations;
			bool methodHasAttribute = annotations.IsInRequiresUnreferencedCodeScope (method);
			if (methodHasAttribute != annotations.IsInRequiresUnreferencedCodeScope (baseMethod)) {
				string message = MessageFormat.FormatRequiresAttributeMismatch (methodHasAttribute,
					baseMethod.DeclaringType.IsInterface, nameof (RequiresUnreferencedCodeAttribute), method.GetDisplayName (), baseMethod.GetDisplayName ());
				Context.LogWarning (method, DiagnosticId.RequiresUnreferencedCodeAttributeMismatch, message);
			}
		}
	}
}
