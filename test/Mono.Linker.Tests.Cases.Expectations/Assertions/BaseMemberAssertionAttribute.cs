using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Delegate, AllowMultiple = true)]
	public abstract class BaseMemberAssertionAttribute : Attribute
	{
	}
}