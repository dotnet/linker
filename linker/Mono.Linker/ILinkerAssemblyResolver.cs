using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Cecil;

namespace Mono.Linker
{
	public interface ILinkerAssemblyResolver : IAssemblyResolver
	{
		void AddSearchDirectory(string directory);

		AssemblyDefinition CacheAssembly(AssemblyDefinition assembly);
	}
}
