using System;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.References
{
	[SandboxDependency ("SupportProjects/TypeForwardersLibrary.dll")]
	[Reference ("TypeForwardersLibrary.dll", true)]
	// TODO: Still need PostCopy step to replace the assembly with type forwarding one
	class UsedTypeForwarderRemovesForwardingAssembly
	{
		public static void Main ()
		{
			var x = new SimpleForwardedClass ();
		}
	}
}
