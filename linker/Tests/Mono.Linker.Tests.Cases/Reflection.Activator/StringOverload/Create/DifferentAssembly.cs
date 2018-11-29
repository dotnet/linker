using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Dependencies;

namespace Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Create {
	[SetupCompileBefore ("other.dll", new [] {"../Dependencies/OtherAssembly.cs"})]
	[KeptMemberInAssembly ("other.dll", typeof (OtherAssembly.Foo), ".ctor()")]
	public class DifferentAssembly {
		public static void Main()
		{
			OtherAssembly.UsedToKeepReferenceAtCompileTime ();
			var tmp = System.Activator.CreateInstance ("other", "Mono.Linker.Tests.Cases.Reflection.Activator.StringOverload.Dependencies.OtherAssembly+Foo");
		}
	}
}