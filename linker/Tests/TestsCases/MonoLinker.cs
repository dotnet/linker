using System;
using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.TestsCases
{
	public class MonoLinker : BaseLinker
	{
		public MonoLinker (TestCase testCase)
			: base (testCase)
		{
		}

		public override void Link (string [] args)
		{
			Driver.Main (args);
		}
	}
}
