// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Collections.Generic;

namespace Mono.Linker {

#if FEATURE_ILLINK
	public class AssemblyResolver : DirectoryAssemblyResolver {
#else
	public class AssemblyResolver : BaseAssemblyResolver {
#endif
		public IDictionary<string, AssemblyDefinition> AssemblyCache { get { throw null;} }
		public AssemblyResolver () { throw null; }
		public bool IgnoreUnresolved { get { throw null; } set { throw null; } }
		public LinkContext Context { get { throw null; } set { throw null; } }
		public override AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters) { throw null; }
		public virtual AssemblyDefinition CacheAssembly (AssemblyDefinition assembly) { throw null; }
		public void AddReferenceAssembly (string referencePath) { throw null; }
		protected override void Dispose (bool disposing) { throw null; }
	}
}
