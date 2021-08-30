using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileBefore (
		"library.dll",
		new[] { "Dependencies/XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1.cs" },
		resources: new object[] { "Dependencies/XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1_NotMatchingName.xml" })]
	[IgnoreDescriptors (false)]

	[KeptResourceInAssembly ("library.dll", "XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1_NotMatchingName.xml")]
	[RemovedMemberInAssembly ("library.dll", typeof (XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1), "Unused()")]
	public class XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly
	{
		public static void Main ()
		{
			XmlFileInReferencedAssemblyIsNotProcessedIfNameDoesNotMatchAnAssembly_Lib1.Used ();
		}
	}
}