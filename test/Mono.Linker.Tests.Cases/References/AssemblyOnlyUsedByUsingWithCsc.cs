using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.References.Dependencies;

namespace Mono.Linker.Tests.Cases.References
{
	/// <summary>
	/// We can't detect the using usage in the assembly.  As a result, nothing in `library` is going to be marked and that assembly will be deleted.
	/// Previously, we used to rewrite copied assemblies that had any references removed -- now copy action should leave the assembly untouched in
	/// the output directory, even if that means having dangling references.
	/// </summary>
	[SetupLinkerAction ("copy", "copied")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Lib.cs" })]

	// When csc is used, `copied.dll` will have a reference to `library.dll`
	[SetupCompileBefore ("copied.dll", new[] { "Dependencies/AssemblyOnlyUsedByUsing_Copied.cs" }, new[] { "library.dll" }, compilerToUse: "csc")]

	[KeptMemberInAssembly ("copied.dll", typeof (AssemblyOnlyUsedByUsing_Copied), "Unused()")]
	[KeptReferencesInAssembly ("copied.dll", new[] { "System.Private.CoreLib", "library" })]

	[RemovedAssembly ("library.dll")]
	[KeptReferencesInAssembly ("copied.dll", new[] { "System.Private.CoreLib", "library" })]
	public class AssemblyOnlyUsedByUsingWithCsc
	{
		public static void Main ()
		{
			// Use something to keep the reference at compile time
			AssemblyOnlyUsedByUsing_Copied.UsedToKeepReference ();
		}
	}
}