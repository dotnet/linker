using System;
using Mono.Linker.Tests.Core;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.CoreIntegration
{
	public class ObjectFactory : BaseObjectFactory
	{
		private readonly BaseAssertions _assertions;

		public ObjectFactory(BaseAssertions assertions)
		{
			_assertions = assertions;
		}

		public override BaseLinker CreateLinker(TestCase testCase)
		{
			return new MonoLinker(testCase);
		}

		public override BaseAssertions CreateAssertions()
		{
			return _assertions;
		}
	}
}
