using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage(AttributeTargets.Class)]
	public class CoreLinkAttribute : Attribute
	{
		public readonly string Value;

		public CoreLinkAttribute(string value)
		{
			Value = value;
		}
	}
}
