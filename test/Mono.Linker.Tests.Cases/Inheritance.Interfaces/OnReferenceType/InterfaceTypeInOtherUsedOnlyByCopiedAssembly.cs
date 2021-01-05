using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Inheritance.Interfaces.Dependencies;

namespace Mono.Linker.Tests.Cases.Inheritance.Interfaces.OnReferenceType
{
	[SetupCompileBefore ("linked.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link) })]
	[SetupCompileBefore ("copy.dll", new[] { typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy) }, new[] { "linked.dll" })]

	[SetupLinkerAction ("copy", "copy")]

#if NETCOREAPP
	[SetupLinkerArgument ("-l", "copy.dll")]
#else
	[SetupLinkerArgument ("-a", "copy")]
#endif

	[KeptTypeInAssembly ("linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo))]
	[KeptMemberInAssembly ("copy.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.A), "Method()")]
	[KeptInterfaceOnTypeInAssembly ("copy.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.A), "linked.dll", typeof (InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Link.IFoo))]
	public class InterfaceTypeInOtherUsedOnlyByCopiedAssembly
	{
		public static void Main ()
		{
			InterfaceTypeInOtherUsedOnlyByCopiedAssembly_Copy.ToKeepReferenceAtCompileTime ();
		}
	}
}