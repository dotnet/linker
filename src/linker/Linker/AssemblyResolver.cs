﻿//
// AssemblyResolver.cs
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
using System.IO;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker
{

	public class AssemblyResolver : DirectoryAssemblyResolver
	{

		readonly Dictionary<string, AssemblyDefinition> _assemblies;
		HashSet<string> _unresolvedAssemblies;
		bool _ignoreUnresolved;
		LinkContext _context;
		readonly HashSet<string> _references;


		public IDictionary<string, AssemblyDefinition> AssemblyCache {
			get { return _assemblies; }
		}

		public AssemblyResolver ()
			: this (new Dictionary<string, AssemblyDefinition> (StringComparer.OrdinalIgnoreCase))
		{
		}

		public AssemblyResolver (Dictionary<string, AssemblyDefinition> assembly_cache)
		{
			_assemblies = assembly_cache;
			_references = new HashSet<string> () { };
		}

		public bool IgnoreUnresolved {
			get { return _ignoreUnresolved; }
			set { _ignoreUnresolved = value; }
		}

		public LinkContext Context {
			get { return _context; }
			set { _context = value; }
		}

		public string GetAssemblyFileName (AssemblyDefinition assembly)
		{
			if (assemblyToPath.TryGetValue (assembly, out string path)) {
				return path;
			}

			// Must be an assembly that we didn't open through the resolver
			return assembly.MainModule.FileName;
		}

		AssemblyDefinition ResolveFromReferences (AssemblyNameReference name, HashSet<string> references, ReaderParameters parameters)
		{
			foreach (var reference in references) {
				foreach (var extension in DirectoryAssemblyResolver.Extensions) {
					var fileName = name.Name + extension;
					if (Path.GetFileName (reference) != fileName)
						continue;
					try {
						return GetAssembly (reference, parameters);
					} catch (BadImageFormatException) {
						continue;
					}
				}
			}

			return null;
		}

		public override AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters)
		{
			// Validate arguments, similarly to how the base class does it.
			if (name == null)
				throw new ArgumentNullException (nameof (name));
			if (parameters == null)
				throw new ArgumentNullException (nameof (parameters));

			if (!_assemblies.TryGetValue (name.Name, out AssemblyDefinition asm) && (_unresolvedAssemblies == null || !_unresolvedAssemblies.Contains (name.Name))) {
				try {
					// Any full path explicit reference takes precedence over other look up logic
					asm = ResolveFromReferences (name, _references, parameters);

					// Fall back to the base class resolution logic
					if (asm == null)
						asm = base.Resolve (name, parameters);

					CacheAssembly (asm);
				} catch (AssemblyResolutionException) {
					if (!_ignoreUnresolved)
						throw;
					_context.LogMessage ($"Ignoring unresolved assembly '{name.Name}'.");
					if (_unresolvedAssemblies == null)
						_unresolvedAssemblies = new HashSet<string> ();
					_unresolvedAssemblies.Add (name.Name);
				}
			}

			return asm;
		}

		public void CacheAssembly (AssemblyDefinition assembly)
		{
			_assemblies[assembly.Name.Name] = assembly;
			if (assembly != null)
				_context.RegisterAssembly (assembly);
		}

		public void AddReferenceAssembly (string referencePath)
		{
			_references.Add (referencePath);
		}

		public HashSet<string> GetReferencePaths ()
		{
			return _references;
		}

		protected override void Dispose (bool disposing)
		{
			foreach (var asm in _assemblies.Values) {
				asm.Dispose ();
			}

			_assemblies.Clear ();
			if (_unresolvedAssemblies != null)
				_unresolvedAssemblies.Clear ();

			base.Dispose (disposing);
		}
	}
}
