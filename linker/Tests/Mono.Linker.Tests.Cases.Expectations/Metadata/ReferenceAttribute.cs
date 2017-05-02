using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata {
	[AttributeUsage (AttributeTargets.Class)]
	public class ReferenceAttribute : Attribute {
		public readonly string Value;

		public ReferenceAttribute (string value)
		{
			Value = value;
		}
	}
}
