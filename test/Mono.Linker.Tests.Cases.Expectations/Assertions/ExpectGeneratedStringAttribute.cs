using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	public class ExpectGeneratedStringAttribute : BaseMemberAssertionAttribute
	{
		public ExpectGeneratedStringAttribute (string expected)
		{
		}
	}
}