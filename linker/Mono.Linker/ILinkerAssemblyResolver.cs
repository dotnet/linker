using System;
using System.Collections;
using Mono.Cecil;

namespace Mono.Linker
{
	public interface ILinkerAssemblyResolver : IAssemblyResolver
	{
		IDictionary AssemblyCache { get; }

		void AddSearchDirectory (string directory);

		AssemblyDefinition CacheAssembly (AssemblyDefinition assembly);

		AssemblyNameReference ResolveName (AssemblyNameReference name);
	}
}
