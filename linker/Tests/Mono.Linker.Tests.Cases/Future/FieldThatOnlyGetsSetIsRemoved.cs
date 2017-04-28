using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.Future
{
	[IgnoreTestCase("We cannot do this yet")]
	class FieldThatOnlyGetsSetIsRemoved
	{
		public static void Main()
		{
			new B().Method();
		}

		class B
		{
			[Removed]
			public int _unused = 3;

			[Kept]
			public void Method() { }
		}
	}
}
