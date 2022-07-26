// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class CheckSuppressionsStep : BaseSubStep
	{
		public override SubStepTargets Targets {
			get {
				return SubStepTargets.Type |
					SubStepTargets.Field |
					SubStepTargets.Method |
					SubStepTargets.Property;
			}
		}

		public override bool IsActiveFor (AssemblyDefinition assembly)
		{
			var assemblyAction = Annotations.GetAction (assembly);
			return assemblyAction == AssemblyAction.Link || assemblyAction == AssemblyAction.Copy;
		}

		public override void ProcessType (TypeDefinition type)
		{
			Context.Suppressions.GatherSuppressions (type);
		}

		public override void ProcessField (FieldDefinition field)
		{
			Context.Suppressions.GatherSuppressions (field);
		}

		public override void ProcessMethod (MethodDefinition method)
		{
			if (Context.Annotations.GetAction (method) != MethodAction.ConvertToThrow)
				Context.Suppressions.GatherSuppressions (method);
		}

		public override void ProcessProperty (PropertyDefinition property)
		{
			Context.Suppressions.GatherSuppressions (property);
		}
	}
}
