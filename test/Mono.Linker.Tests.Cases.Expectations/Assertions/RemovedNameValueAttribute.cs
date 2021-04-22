using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	/// <summary>
	/// Verifies that literal value of the member is removed
	/// </summary>
	[AttributeUsage (AttributeTargets.All, AllowMultiple = false, Inherited = false)]
	public class RemovedNameValueAttribute : BaseExpectedLinkedBehaviorAttribute
	{
	}
}