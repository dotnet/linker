// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Mono.Linker {

	public enum AssemblyAction {
		// Ignore the assembly
		Skip,
		// Copy the existing files, assembly and symbols, into the output destination. E.g. .dll and .mdb
		// The linker still analyzes the assemblies (to know what they require) but does not modify them.
		Copy,
		// Copy the existing files, assembly and symbols, into the output destination if and only if
		// anything from the assembly is used.
		// The linker still analyzes the assemblies (to know what they require) but does not modify them.
		CopyUsed,
		// Link the assembly
		Link,
		// Remove the assembly from the output
		Delete,
		// Save the assembly/symbols in memory without linking it. 
		// E.g. useful to remove unneeded assembly references (as done in SweepStep), 
		//  resolving [TypeForwardedTo] attributes (like PCL) to their final location
		Save,
		// Keep all types, methods, and fields but add System.Runtime.BypassNGenAttribute to unmarked methods.
		AddBypassNGen,
		// Keep all types, methods, and fields in marked assemblies but add System.Runtime.BypassNGenAttribute to unmarked methods.
		// Delete unmarked assemblies.
		AddBypassNGenUsed
	}
}
