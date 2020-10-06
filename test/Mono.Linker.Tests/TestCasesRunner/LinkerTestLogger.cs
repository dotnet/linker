using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ConsoleLogger
	{
		public LinkerTestLogger (MessageCategory categoriesToCache) :
			base (null, categoriesToCache)
		{
		}

		public IEnumerable<MessageContainer> Messages => GetCachedMessages ();
	}
}