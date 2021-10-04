﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public interface ISubStep
	{
		SubStepTargets Targets { get; }

		void Initialize (LinkContext context);
		bool IsActiveFor (AssemblyDefinition assembly);

		void ProcessAssembly (AssemblyDefinition assembly);
		void ProcessType (TypeDefinition type);
		void ProcessField (FieldDefinition field);
		void ProcessMethod (MethodDefinition method);
		void ProcessProperty (PropertyDefinition property);
		void ProcessEvent (EventDefinition @event);
	}
}
