using System;
using Mono.Cecil;
using Mono.Linker;

public class Utils
{
	public static bool IsManagedAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return true;
		} catch (BadImageFormatException) {
			return false;
		}
	}

	public static bool IsReadyToRunAssembly (string fileName)
	{
		try {
			ModuleDefinition module = ModuleDefinition.ReadModule (fileName);
			return AssemblyUtilities.IsReadyToRun (module);
		} catch (BadImageFormatException) {
			return false;
		}
	}
}
