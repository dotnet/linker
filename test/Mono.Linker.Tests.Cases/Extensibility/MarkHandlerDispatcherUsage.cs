using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
#if !NETCOREAPP
	[IgnoreTestCase ("Specific to the illink build")]
#endif
	[SetupCompileBefore ("MyMarkHandlerDispatcher.dll", new[] { "Dependencies/MyMarkHandlerDispatcher.cs", "Dependencies/CustomSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "MyMarkHandlerDispatcher,MyMarkHandlerDispatcher.dll")]
	public class MarkHandlerDispatcherUsage
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