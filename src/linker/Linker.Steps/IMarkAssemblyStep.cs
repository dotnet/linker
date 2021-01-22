// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker.Steps
{

	/// <summary>
	/// Extensibility point for custom steps that run during MarkStep,
	/// for every marked assembly.
	/// </summary>
	public interface IMarkAssemblyStep
	{
		/// <summary>
		/// Initialize the per-assembly step. The intended use is just to do
		/// simple setup of global state. The main processing logic should be
		/// done by ProcessAssembly. Initialize is called at the beginning of
		/// pipeline processing, before any of the normal pipeline steps (IStep) run.
		/// </summary>
		void Initialize (LinkContext context);

		/// <summary>
		/// Process an assembly. This should perform the main logic of the step,
		/// including any modifications to the assembly or to the global Annotations
		/// state. ProcessAssembly is called exactly once, when an assembly is marked.
		/// It is called before the main MarkStep processing happens for the assembly,
		/// but other steps may already have marked parts of the assembly through Annotations.
		/// </summary>
		void ProcessAssembly (AssemblyDefinition assembly);
	}
}
