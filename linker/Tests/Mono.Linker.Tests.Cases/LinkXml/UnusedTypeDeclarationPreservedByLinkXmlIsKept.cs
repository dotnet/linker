using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.LinkXml {
	public class UnusedTypeDeclarationPreservedByLinkXmlIsKept {
		public static void Main ()
		{
		}
	}

	[Kept]
	[KeptMember (".ctor()")]
	[KeptBaseType (typeof (UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase))]
	class UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedType : UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase 
	{
		int field;
		static void Method ()
		{
		}

		string Prop { get; set;  }
	}

	[Kept]
	[KeptMember (".ctor()")]
	class UnusedTypeDeclarationPreservedByLinkXmlIsKeptUnusedTypeBase
	{
	}
}
