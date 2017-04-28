using System;
using Mono.Linker.Tests.Core.Base;

namespace Mono.Linker.Tests.CoreIntegration
{
	public class XUnitAssertions : BaseAssertions
	{
		public override void IsNull(object obj, string message)
		{
			throw new NotImplementedException();
		}

		public override void IsNotNull(object obj, string message)
		{
			throw new NotImplementedException();
		}

		public override void IsTrue(bool value, string message)
		{
			throw new NotImplementedException();
		}

		public override void Ignore(string reason)
		{
			throw new NotImplementedException();
		}

		public override void Fail(string message)
		{
			throw new NotImplementedException();
		}

		public override void AreEqual(object expected, object actual, string message)
		{
			throw new NotImplementedException();
		}
	}
}
