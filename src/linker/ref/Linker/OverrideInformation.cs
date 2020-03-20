// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
