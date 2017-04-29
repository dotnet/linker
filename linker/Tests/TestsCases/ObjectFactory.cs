using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.TestsCases
{
	public class ObjectFactory : BaseObjectFactory
	{
		public ObjectFactory ()
		{
		}

		public override BaseLinker CreateLinker (TestCase testCase)
		{
			return new MonoLinker (testCase);
		}
	}
}
