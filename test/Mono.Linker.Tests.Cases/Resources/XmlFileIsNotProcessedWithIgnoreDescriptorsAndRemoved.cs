using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (true)]
	[StripDescriptors (true)]

	[SetupCompileResource ("Dependencies/XmlFileIsNotProcessedWithIgnoreDescriptorsAndRemoved.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	[RemovedResourceInAssembly ("test.exe", "ILLink.Descriptors.xml")]
	public class XmlFileIsNotProcessedWithIgnoreDescriptorsAndRemoved
	{
		public static void Main ()
		{
		}

		public class Unused
		{
		}
	}
}
