using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.Resources.Dependencies;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupCompileResource ("Dependencies/XmlFileWithTypePreserve1.xml", "ILLink.Descriptors.xml")]
	[SetupCompileBefore ("library.dll",
		new string[] { "Dependencies/XmlFileWithTypePreserve_Lib.cs" },
		resources: new object[] {
			new string[] { "Dependencies/XmlFileWithTypePreserve2.xml", "ILLink.Descriptors.xml" }
	})]
	[IgnoreDescriptors (false)]
	[KeptAssembly ("library.dll")]
	public class XmlFileWithTypePreserve
	{
		public static void Main ()
		{
			EmbeddedLinkXmlFileWithTypePreserve_Lib.Method ();
		}

		[Kept]
		[KeptMember (".ctor()")]
		class PreservedType
		{
			[Kept]
			static bool field;

			[Kept]
			static void Method () { }
		}
	}
}
