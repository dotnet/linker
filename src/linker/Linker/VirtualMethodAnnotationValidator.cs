// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Mono.Cecil;

namespace Mono.Linker
{
	/// <summary>
	/// This class validates consistency of annotations for virtual methods.
	/// Typically the base method and its override must have the exact same annotations.
	/// This also applies to interface method and its implementation.
	/// </summary>
	public class VirtualMethodAnnotationValidator
	{
		readonly LinkContext _context;
		readonly HashSet<MethodDefinition> _virtualMethodsWithAnnotationsToValidate;

		public VirtualMethodAnnotationValidator (LinkContext context)
		{
			_context = context;
			_virtualMethodsWithAnnotationsToValidate = new HashSet<MethodDefinition> ();
		}

		public void EnqueueVirtualMethod (MethodDefinition method)
		{
			if (!method.IsVirtual)
				return;

			if (_context.Annotations.FlowAnnotations.RequiresDataFlowAnalysis (method) ||
				_context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method))
				_virtualMethodsWithAnnotationsToValidate.Add (method);
		}

		public void Validate ()
		{
			foreach (var method in _virtualMethodsWithAnnotationsToValidate) {
				var baseMethods = _context.Annotations.GetBaseMethods (method);
				if (baseMethods != null) {
					foreach (var baseMethod in baseMethods) {
						_context.Annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (method, baseMethod);
						ValidateMethodRequiresUnreferencedCodeAreSame (method, baseMethod);
					}
				}

				var overrides = _context.Annotations.GetOverrides (method);
				if (overrides != null) {
					foreach (var overrideInformation in overrides) {
						// Skip validation for cases where both base and override are in the list, we will validate the edge
						// when validating the override from the list.
						// This avoids validating the edge twice (it would produce the same warning twice)
						if (_virtualMethodsWithAnnotationsToValidate.Contains (overrideInformation.Override)) {
							System.Console.WriteLine (overrideInformation.Override.GetDisplayName ());
							continue;
						}

						_context.Annotations.FlowAnnotations.ValidateMethodAnnotationsAreSame (overrideInformation.Override, method);
						ValidateMethodRequiresUnreferencedCodeAreSame (overrideInformation.Override, method);
					}
				}
			}
		}

		void ValidateMethodRequiresUnreferencedCodeAreSame (MethodDefinition method, MethodDefinition baseMethod)
		{
			if (_context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (method) !=
				_context.Annotations.HasLinkerAttribute<RequiresUnreferencedCodeAttribute> (baseMethod))
				_context.LogWarning (
					$"Presence of RequiresUnreferencedCodeAttribute on method '{method.GetDisplayName ()}' doesn't match overridden method '{baseMethod.GetDisplayName ()}'. " +
					$"All overridden methods must have RequiresUnreferencedCodeAttribute.",
					2046,
					method);
		}
	}
}
