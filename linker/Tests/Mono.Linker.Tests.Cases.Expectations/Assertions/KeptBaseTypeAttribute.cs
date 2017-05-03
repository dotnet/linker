using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	public sealed class KeptBaseTypeAttribute : KeptAttribute
	{
		public readonly string TypeName;

		public KeptBaseTypeAttribute (string typeName)
		{
			TypeName = typeName;
		}
	}
}