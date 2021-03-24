using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Helpers;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	/// <summary>
	/// Although we can't detect the using usage, there is a typeref pointing to `library` in the `copied` assembly (that has `copy` action), 
	/// causing the linker to keep `library`.
	/// Because of that, `copied` needs to have it's reference to `library` removed even though we specified an assembly action of `copy`
	/// </summary>
	[SetupLinkerAction ("copy", "copied")]

	// --keep-facades sends the sweep step down a different code path that caused problems for this corner case
	[SetupLinkerArgument ("--keep-facades", "true")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Lib.cs" })]

	// When csc is used, `copied.dll` will have a reference to `library.dll`
	[SetupCompileBefore ("copied.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Copied.cs" }, new[] { "library.dll" }, compilerToUse: "csc")]

	// Here to assert that the test is setup correctly to copy the copied assembly.  This is an important aspect of the bug
	[KeptMemberInAssembly ("copied.dll", typeof (AssemblyOnlyUsedByUsing_Copied), "Unused()")]

	// There is a type reference coming from a copy assembly, so `library` is kept.
	[KeptAssembly ("library.dll")]
	[KeptReferencesInAssembly ("copied.dll", new [] { PlatformAssemblies.CoreLib, "library" })]
	public class AssemblyOnlyUsedByUsingWithCscWithKeepFacades
	{
		public static void Main ()
		{
			// Use something to keep the reference at compile time
			AssemblyOnlyUsedByUsing_Copied.UsedToKeepReference ();
		}
	}
}