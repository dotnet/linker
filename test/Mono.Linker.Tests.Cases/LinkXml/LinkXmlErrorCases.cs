using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.LinkXml
{
	[SetupLinkerDescriptorFile ("LinkXmlErrorCases.xml")]
	[SetupLinkerArgument ("--skip-unresolved", "true")]

	[ExpectedWarning ("IL2007", "NonExistentAssembly", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2008", "NonExistentType", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2009", "NonExistentMethod", "TypeWithNoMethods", FileName = "LinkXmlErrorCases.xml")]
	[ExpectedWarning ("IL2012", "NonExistentField", "TypeWithNoFields", FileName = "LinkXmlErrorCases.xml")]
	class LinkXmlErrorCases
	{
		public static void Main ()
		{
		}

		[Kept]
		[ExpectedWarning ("IL2001", "TypeWithNoFields")]
		class TypeWithNoFields
		{
			private void Method () { }
		}

		[Kept]
		[ExpectedWarning ("IL2002", "TypeWithNoMethods")]
		struct TypeWithNoMethods
		{
		}
	}
}