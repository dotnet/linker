using System.Runtime.InteropServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Interop.PInvoke.Individual
{
	[SetupLinkerArgument ("--output-pinvokes", new [] { "pinvokes.json" })]

	public class CanOutputPInvokes
	{
		public static void Main ()
		{
			var foo = FooEntryPoint ();
			var bar = CustomEntryPoint ();
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