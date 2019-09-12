using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.LinkXml.Dependencies.EmbeddedLinkXmlFromCopyAssemblyIsProcessed;

namespace Mono.Linker.Tests.Cases.LinkXml {
	[SetupCompileBefore ("Library.dll",
		new [] { "Dependencies/EmbeddedLinkXmlFromCopyAssemblyIsProcessed/OtherLibrary.cs" })]
	[SetupCompileBefore ("CopyLibrary.dll",
		new [] { "Dependencies/EmbeddedLinkXmlFromCopyAssemblyIsProcessed/CopyLibrary.cs" },
		resources: new [] { "Dependencies/EmbeddedLinkXmlFromCopyAssemblyIsProcessed/CopyLibrary.xml"})]
	[IncludeBlacklistStep (true)]
	[SetupLinkerAction ("copy", "CopyLibrary")]

	[KeptTypeInAssembly ("CopyLibrary.dll", typeof (CopyLibrary))]
	[KeptTypeInAssembly ("Library.dll", typeof (OtherLibrary))]
	public class EmbeddedLinkXmlFromCopyAssemblyIsProcessed {
		public static void Main ()
		{
			var tmp = new CopyLibrary ();
			tmp.Method ();
		}
	}
}