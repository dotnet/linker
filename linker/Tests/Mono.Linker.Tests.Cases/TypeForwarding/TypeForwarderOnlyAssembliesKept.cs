using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.TypeForwarding.Dependencies;

namespace Mono.Linker.Tests.Cases.TypeForwarding {
	[KeepTypeForwarderOnlyAssemblies ("true")]
	[CompileAssemblyBefore ("Forwarder.dll", new[] { "Dependencies/ReferenceImplementationLibrary.cs" }, defines: new[] { "INCLUDE_REFERENCE_IMPL" })]

	// After compiling the test case we then replace the reference impl with implementation + type forwarder
	[CompileAssemblyAfter ("Implementation.dll", new[] { "Dependencies/ImplementationLibrary.cs" })]
	[CompileAssemblyAfter ("Forwarder.dll", new[] { "Dependencies/ForwarderLibrary.cs" }, references: new[] { "Implementation.dll" }, defines: new[] { "INCLUDE_FORWARDERS" })]
	[KeptAssembly ("Implementation.dll")]
	[KeptAssembly ("Forwarder.dll")]
	[KeptMemberInAssembly ("Implementation.dll", typeof (ImplementationLibrary), "GetSomeValue()")]
	public class TypeForwarderOnlyAssembliesKept {
		static void Main ()
		{
			Console.WriteLine (new ImplementationLibrary ().GetSomeValue ());
		}
	}
}
