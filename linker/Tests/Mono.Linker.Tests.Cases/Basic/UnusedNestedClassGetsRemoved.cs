using System;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Basic
{
	public class UnusedNestedClassGetsRemoved
	{
		public static void Main()
		{
		}

		[Removed]
		class Unused { }
	}
}
