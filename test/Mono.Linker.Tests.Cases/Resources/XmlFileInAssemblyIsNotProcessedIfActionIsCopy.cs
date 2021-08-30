using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileBefore (
		"XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll",
		new[] { "Dependencies/XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.cs" },
		resources: new object[] { "Dependencies/XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.xml" })]
	[SetupLinkerAction ("copy", "XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1")]
	[IgnoreDescriptors (false)]

	[KeptResourceInAssembly ("XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll", "XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.xml")]
	[KeptMemberInAssembly ("XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.dll", typeof (XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1), "Unused()")]
	public class XmlFileInAssemblyIsNotProcessedIfActionIsCopy
	{
		public static void Main ()
		{
			XmlFileInAssemblyIsNotProcessedIfActionIsCopy_Lib1.Used ();
		}
	}
}