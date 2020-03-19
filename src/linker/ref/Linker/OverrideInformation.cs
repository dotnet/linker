using System.Diagnostics;
using Mono.Cecil;

namespace Mono.Linker {
	[DebuggerDisplay ("{Override}")]
	public class OverrideInformation {
		public readonly MethodDefinition Base;
		public readonly MethodDefinition Override;
		public readonly InterfaceImplementation MatchingInterfaceImplementation;
		public OverrideInformation (MethodDefinition @base, MethodDefinition @override, InterfaceImplementation matchingInterfaceImplementation = null) { throw null; }
		public bool IsOverrideOfInterfaceMember { get { throw null; } }
		public TypeDefinition InterfaceType { get { throw null; } }
	}
}
