﻿using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Resources
{
	[SetupLinkerCoreAction ("link")]
	[IgnoreDescriptors (false)]

	[SetupCompileResource ("Dependencies/EmbeddedLinkXmlFileIsProcessed.xml", "ILLink.Descriptors.xml")]
	[SkipPeVerify]
	public class EmbeddedLinkXmlFileIsProcessed
	{
		public static void Main ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		public class Unused
		{
		}
	}
}
