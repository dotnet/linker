using System;

namespace Mono.Linker {
	[Flags]
	public enum AssemblyActionFlag : int {
		// If there is any reference to a type, preserve all members in that type
		TypeGranularity = 1,
	}
}
