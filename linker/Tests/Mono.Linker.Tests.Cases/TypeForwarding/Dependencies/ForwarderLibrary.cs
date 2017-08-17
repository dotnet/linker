using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

#if INCLUDE_FORWARDERS

[assembly: System.Runtime.CompilerServices.TypeForwardedTo (typeof (Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary))]

#endif

#if !INCLUDE_FORWARDERS
namespace Mono.Linker.Tests.Cases.TypeForwarding.Dependencies {
	// Need something here to keep the testing infrastructure happy
	[NotATestCase]
	public class ForwarderLibrary {
	}
}
#endif
