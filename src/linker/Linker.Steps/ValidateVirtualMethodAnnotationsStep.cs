﻿// Licensed to the .NET Foundation under one or more agreements.
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
			foreach (var method in Context.Annotations.VirtualMethodsWithAnnotationsToValidate) {
				var baseMethods = Context.Annotations.GetBaseMethods (method);
				if (baseMethods != null) {
					foreach (var baseMethod in baseMethods) {
						Context.Annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (method, baseMethod);
						ValidateMethodRequiresUnreferencedCodeAreSame (method, baseMethod);
					}
				}

				var overrides = Context.Annotations.GetOverrides (method);
				if (overrides != null) {
					foreach (var overrideInformation in overrides) {
						// Skip validation for cases where both base and override are in the list, we will validate the edge
						// when validating the override from the list.
						// This avoids validating the edge twice (it would produce the same warning twice)
						if (Context.Annotations.VirtualMethodsWithAnnotationsToValidate.Contains (overrideInformation.Override))
							continue;

						Context.Annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (overrideInformation.Override, method);
						ValidateMethodRequiresUnreferencedCodeAreSame (overrideInformation.Override, method);
					}
				}
			}
		}

		void ValidateMethodRequiresUnreferencedCodeAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			if (Context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method) !=
				Context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (baseMethod))
				Context.LogWarning (
					$"Presence of RequiresUnreferencedCodeAttribute on method '{method.GetDisplayName ()}' doesn't match overridden method '{baseMethod.GetDisplayName ()}'. " +
					$"All overridden methods must have RequiresUnreferencedCodeAttribute.",
					2046,
					method);
		}
	}
}
