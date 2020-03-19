using System;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using Mono.Collections.Generic;
using Mono.Cecil;

#if FEATURE_ILLINK
namespace Mono.Linker {
	public abstract class DirectoryAssemblyResolver : IAssemblyResolver {
		public void AddSearchDirectory (string directory) { throw null; }
		public void RemoveSearchDirectory (string directory) { throw null; }
		public string [] GetSearchDirectories () { throw null; }
		protected DirectoryAssemblyResolver () { throw null; }
		protected AssemblyDefinition GetAssembly (string file, ReaderParameters parameters) { throw null; }
		public virtual AssemblyDefinition Resolve (AssemblyNameReference name) { throw null; }
		public virtual AssemblyDefinition Resolve (AssemblyNameReference name, ReaderParameters parameters) { throw null; }
		public void Dispose () { throw null; }
		protected virtual void Dispose (bool disposing) { throw null; }
	}
}
#endif
