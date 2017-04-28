using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.CoreIntegration
{
	public class MonoLinker : BaseLinker
	{
		public MonoLinker(TestCase testCase)
			: base(testCase)
		{
		}

		public override void Link(string[] args)
		{
			Driver.Main(args);
		}
	}
}
