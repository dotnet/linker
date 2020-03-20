// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace Mono.Linker
{
	public class KnownMembers
	{
		public MethodDefinition NotSupportedExceptionCtorString { get { throw null; } set { throw null; } }
		public MethodDefinition DisablePrivateReflectionAttributeCtor { get { throw null; } set { throw null; } }
		public MethodDefinition ObjectCtor { get { throw null; } set { throw null; } }
		public static bool IsNotSupportedExceptionCtorString (MethodDefinition method) { throw null; }
		public static bool IsSatelliteAssemblyMarker (MethodDefinition method) { throw null; }
	}
}
