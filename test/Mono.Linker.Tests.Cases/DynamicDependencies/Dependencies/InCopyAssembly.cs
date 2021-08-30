using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies
{
	public class InCopyAssembly
	{
		[DynamicDependency ("ExtraMethod1")]
		public InCopyAssembly ()
		{
		}

		static void ExtraMethod1 ()
		{
		}
	}
}
