using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[IgnoreDescriptors (true)]
	[StripDescriptors (false)]

	[SetupCompileResource ("Dependencies/XmlFileIsNotProcessedWithIgnoreDescriptors.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	[KeptResource ("ILLink.Descriptors.xml")]
	public class XmlFileIsNotProcessedWithIgnoreDescriptors
	{
		public static void Main ()
		{
		}

		public class Unused
		{
		}
	}
}
