using System;
using System.Collections.Generic;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkAttributes
{
	[SetupLinkAttributesFile ("LinkAttributeErrorCases.xml")]
	[IgnoreLinkAttributes (false)]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2007", "NonExistentAssembly2", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2030", "NonExistentAssembly1", FileName = "LinkAttributeErrorCases.xml")]
	[ExpectedWarning ("IL2030", "MalformedAssemblyName, thisiswrong", FileName = "LinkAttributeErrorCases.xml")]
	class LinkAttributeErrorCases
	{
		public static void Main ()
		{

		}
	}
}
