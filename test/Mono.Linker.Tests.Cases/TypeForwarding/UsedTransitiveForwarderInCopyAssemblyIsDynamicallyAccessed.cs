// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	[SetupCompileBefore ("SecondForwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]
	[SetupCompileBefore ("FirstForwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "SecondForwarder.dll" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[SetupCompileAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[SetupCompileAfter ("SecondForwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" })]
	[SetupLinkerAction ("copy", "FirstForwarder")]

	[KeptMemberInAssembly ("FirstForwarder.dll", typeof (ImplementationLibrary))]
	// Dynamically accessing a type forwarder T in a copy assembly will cause the linker
	// to keep the whole chain of forwarders from T to the type S it resolves to, since we
	// cannot rewrite assembly references in the facade, and droping anything between T
	// and S would break functionality.
	[KeptMemberInAssembly ("SecondForwarder.dll", typeof (ImplementationLibrary))]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	class UsedTransitiveForwarderInCopyAssemblyIsDynamicallyAccessed
	{
		static void Main ()
		{
			// [copy]            [link]             [link]
			// FirstForwarder -> SecondForwarder -> Implementation
			PointToTypeInFacade ("Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary, FirstForwarder");
		}

		[Kept]
		static void PointToTypeInFacade (
			[KeptAttributeAttribute (typeof(DynamicallyAccessedMembersAttribute))]
			[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] string typeName)
		{
		}
	}
}
