using System;

namespace Mono.Linker.Tests.Cases.Expectations.Assertions
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Delegate, AllowMultiple = true, Inherited = false)]
	public class KeptTypeInAssemblyAttribute : KeptAttribute
	{
		public readonly string AssemblyName;
		public readonly string FullTypeName;

		public KeptTypeInAssemblyAttribute(string assemblyName, string fullTypeName)
		{
			AssemblyName = assemblyName;
			FullTypeName = fullTypeName;
		}
	}
}
