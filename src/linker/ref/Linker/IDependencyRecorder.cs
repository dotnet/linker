// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker
{
	/// <summary>
	/// Abstraction exposed by the linker (mostly MarkStep, but not only) - it will call this interface
	/// every time it finds a dependency between two parts of the dependency graph.
	/// </summary>
	public interface IDependencyRecorder
	{
		/// <summary>
		/// Reports a dependency detected by the linker.
		/// </summary>
		/// <param name="source">The source of the dependency (for example the caller method).</param>
		/// <param name="target">The target of the dependency (for example the callee method).</param>
		/// <param name="marked">true if the target is also marked by the MarkStep.</param>
		/// <remarks>The source and target are typically Cecil metadata objects (MethodDefinition, TypeDefinition, ...)
		/// but they can also be the linker steps or really any other object.</remarks>
		void RecordDependency (object source, object target, bool marked);
	}
}
