using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Extensibility
{
#if !NETCOREAPP
	[IgnoreTestCase ("Specific to the illink build")]
#endif
	[SetupCompileBefore ("customstep.dll", new[] { "Dependencies/PreserveMethodsSubStep.cs" }, new[] { "illink.dll", "Mono.Cecil.dll", "netstandard.dll" })]
	[SetupLinkerArgument ("--custom-step", "+MarkStep:PreserveMethodsSubStep,customstep.dll")]
	public class CustomStepCanPreserveMethodsAfterMark
	{
		public static void Main ()
		{
			UsedType.UsedMethod ();
		}

		[Kept]
		static class UsedType {
			[Kept]
			public static void UsedMethod () { }

			[Kept]
			public static void PreservedForType () { }

			[Kept]
			public static void PreservedForMethod_UsedMethod () { }
		}

	}
}