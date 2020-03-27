using System;

namespace Mono.Linker.Tests.Cases.Expectations.Metadata
{
	[AttributeUsage (AttributeTargets.Assembly, AllowMultiple = false)]
	public class DisableKeptItemsValidationAttribute : BaseMetadataAttribute
	{
		public DisableKeptItemsValidationAttribute () { }
	}
}
