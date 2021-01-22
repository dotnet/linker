// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public abstract class MarkAssemblySubStepsDispatcher : IMarkAssemblyStep
	{
		protected MarkAssemblySubStepsDispatcher () => throw null;

		protected MarkAssemblySubStepsDispatcher (IEnumerable<ISubStep> subSteps) => throw null;

		public void Add (ISubStep substep) => throw null;

		void IMarkAssemblyStep.Initialize (LinkContext context) => throw null;
		void IMarkAssemblyStep.ProcessAssembly (AssemblyDefinition assembly) => throw null;
	}
}