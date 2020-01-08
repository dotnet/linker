using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
	public class RecognizedReflectionAccessPatternAttribute : BaseExpectedLinkedBehaviorAttribute
	{
		public RecognizedReflectionAccessPatternAttribute (string sourceMethod, string reflectionMethod, string accessedItem)
		{
			if (string.IsNullOrEmpty(sourceMethod))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (sourceMethod));

			if (string.IsNullOrEmpty (reflectionMethod))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (reflectionMethod));

			if (string.IsNullOrEmpty (accessedItem))
				throw new ArgumentException ("Value cannot be null or empty.", nameof (accessedItem));
		}
	}
}
