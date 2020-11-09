//
// LoadReferencesStep.cs
//
// Author:
//   Jb Evain (jbevain@novell.com)
//
// (C) 2007 Novell, Inc.
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using Mono.Cecil;

namespace Mono.Linker.Steps
{
	public class LoadReferencesStep : IAssemblyStep
	{
		LinkContext _context;

		readonly HashSet<AssemblyNameDefinition> references = new HashSet<AssemblyNameDefinition> ();

		readonly HashSet<AssemblyDefinition> newReferences = new HashSet<AssemblyDefinition> ();

		public void Initialize (LinkContext context)
		{
			_context = context;
		}

		public virtual void ProcessAssemblies (HashSet<AssemblyDefinition> assemblies)
		{
			newReferences.Clear ();

			foreach (var assembly in assemblies)
				ProcessReferences (assembly);

			// Ensure that subsequent IAssemblySteps only process assemblies
			// which have not already been processed.
			assemblies.Clear ();
			foreach (var assembly in newReferences)
				assemblies.Add (assembly);
		}

		protected void ProcessReferences (AssemblyDefinition assembly)
		{
			if (!references.Add (assembly.Name))
				return;

			newReferences.Add (assembly);

			_context.RegisterAssembly (assembly);

			foreach (AssemblyDefinition referenceDefinition in _context.ResolveReferences (assembly)) {
				try {
					ProcessReferences (referenceDefinition);
				} catch (Exception ex) {
					throw new LinkerFatalErrorException (
						MessageContainer.CreateErrorMessage ($"Assembly '{assembly.FullName}' cannot be loaded due to failure in processing '{referenceDefinition.FullName}' reference", 1010), ex);
				}
			}
		}
	}
}
