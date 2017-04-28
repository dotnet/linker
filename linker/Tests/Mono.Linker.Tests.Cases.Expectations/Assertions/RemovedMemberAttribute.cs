using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public sealed class RemovedMemberAttribute : RemovedAttribute
	{
		public readonly string Name;

		public RemovedMemberAttribute(string name)
		{
			Name = name;
		}
	}
}
