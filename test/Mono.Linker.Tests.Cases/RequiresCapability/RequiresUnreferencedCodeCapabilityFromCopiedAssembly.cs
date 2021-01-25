﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.RequiresCapability.Dependencies;

namespace Mono.Linker.Tests.Cases.RequiresCapability
{
	[SetupLinkerAction ("copy", "lib")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/RequiresUnreferencedCodeInCopyAssembly.cs" })]
	[KeptAllTypesAndMembersInAssembly ("lib.dll")]
	[LogDoesNotContain ("IL2026")]
	[LogDoesNotContain ("IL2027")]
	public class RequiresUnreferencedCodeCapabilityFromCopiedAssembly
	{
		public static void Main ()
		{
			Test ();
		}

		[Kept]
		static void Test ()
		{
			var x = new RequiresUnreferencedCodeInCopyAssembly ();
		}
	}
}
