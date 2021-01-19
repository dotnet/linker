using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Libraries.Dependencies;

namespace Mono.Linker.Tests.Cases.Libraries
{
#if !NETCOREAPP
	[IgnoreTestCase ("Correctly handled by illink only")]
#endif
	[Kept]
	[KeptMember (".ctor()")]
	[SetupLinkerAction ("copyused", "test")]
	[SetupCompileBefore ("lib.dll", new[] { "Dependencies/CopyUsedAssemblyWithMainEntryRoot_Lib.cs" })]
	[KeptTypeInAssembly ("lib.dll", typeof (CopyUsedAssemblyWithMainEntryRoot_Lib))]
	public class CopyUsedAssemblyWithMainEntryRoot
	{
		[Kept]
		public static void Main ()
		{
		}

		[Kept]
		public void UnusedPublicMethod ()
		{
		}

		[Kept]
		private void UnusedPrivateMethod ()
		{
			new CopyUsedAssemblyWithMainEntryRoot_Lib ();
		}
	}
}