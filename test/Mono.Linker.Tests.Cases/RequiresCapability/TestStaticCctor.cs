

using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

public static class TestStaticCctorRequires
{
	class StaticCtor
	{
		[ExpectedWarning ("IL2116", "StaticCtor..cctor()", ProducedBy = ProducedBy.Trimmer)]
		[RequiresUnreferencedCode ("Message for --TestStaticCtor--")]
		static StaticCtor ()
		{
		}
	}

	static void Main ()
	{
		_ = new StaticCtor ();
	}
}
