using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Interop.PInvoke.Individual.Dependencies;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Individual
{
	[SetupLinkerAction ("copy", "copyassembly")]
	[SetupCompileBefore ("copyassembly.dll", new [] { typeof (CanOutputPInvokes_CopyAssembly) })]
	[SetupLinkerArgument ("--output-pinvokes", new [] { "pinvokes.json" })]

	public class CanOutputPInvokes
	{
		public static void Main ()
		{
			var foo = FooEntryPoint ();
			var bar = CustomEntryPoint ();

			var copyAssembly = new CanOutputPInvokes_CopyAssembly ();
		}

		class Foo
		{
			public Foo ()
			{
			}
		}

		[DllImport ("lib")]
		private static extern Foo FooEntryPoint ();

		[DllImport ("lib", EntryPoint = "CustomEntryPoint")]
		private static extern Foo CustomEntryPoint ();
	}
}