using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("UsedNonRequiredExportedTypeIsKept.xml")]

	[SetupCompileBefore ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("lib.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_lib.cs" })]
	[SetupCompileAfter ("libfwd.dll", new[] { "Dependencies/UsedNonRequiredExportedTypeIsKept_fwd.cs" }, references: new[] { "lib.dll" })]
	[SetupLinkerAction ("copy", "libfwd")]

	[KeptMemberInAssembly ("lib.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used1), "field", ExpectationAssemblyName = "libfwd.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used2), "Method()", ExpectationAssemblyName = "libfwd.dll")]
	[KeptMemberInAssembly ("lib.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used3), "Method()", ExpectationAssemblyName = "libfwd.dll")]
	[KeptTypeInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used1))]
	[KeptTypeInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used2))]
	[KeptTypeInAssembly ("libfwd.dll", typeof (UsedNonRequiredExportedTypeIsKept_Used3))]

	public class UsedNonRequiredExportedTypeIsKept
	{
		public static void Main ()
		{
			var tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used1).ToString ();
			tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used2).ToString ();
			tmp = typeof (UsedNonRequiredExportedTypeIsKept_Used3).ToString ();
		}
	}
}