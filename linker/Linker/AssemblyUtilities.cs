using System;
using Mono.Cecil;

namespace Mono.Linker {

	public class AssemblyUtilities {

		public static bool IsReadyToRun (ModuleDefinition module)
		{
			return (module.Attributes & ModuleAttributes.ILOnly) == 0 &&
				(module.Attributes & ModuleAttributes.ILLibrary) != 0;
		}

	}

}
