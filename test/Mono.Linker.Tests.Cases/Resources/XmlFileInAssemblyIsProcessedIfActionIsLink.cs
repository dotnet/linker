using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileBefore (
		"XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.dll",
		new[] { "Dependencies/XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.cs" },
		resources: new object[] { "Dependencies/XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.xml" })]
	[SetupLinkerAction ("link", "XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1")]
	[IgnoreDescriptors (false)]

	[RemovedResourceInAssembly ("XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.dll", "XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.xml")]
	[KeptMemberInAssembly ("XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.dll", typeof (XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1), "Unused()")]
	public class XmlFileInAssemblyIsProcessedIfActionIsLink
	{
		public static void Main ()
		{
			XmlFileInAssemblyIsProcessedIfActionIsLink_Lib1.Used ();
		}
	}
}