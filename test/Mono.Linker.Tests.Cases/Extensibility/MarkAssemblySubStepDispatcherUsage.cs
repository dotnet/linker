using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
#if !NETCOREAPP
	[IgnoreTestCase ("Specific to the illink build")]
#endif
	[SetupCompileBefore ("MyMarkAssemblyDispatcher.dll", new[] { "Dependencies/MyMarkAssemblyDispatcher.cs", "Dependencies/CustomSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "MyMarkAssemblyDispatcher,MyMarkAssemblyDispatcher.dll")]
	public class MarkAssemblySubStepDispatcherUsage
	{
		public static void Main ()
		{
		}

		[Kept]
		public class NestedType
		{
			public int field;

			public static void SomeMethod ()
			{
			}
		}
	}
}