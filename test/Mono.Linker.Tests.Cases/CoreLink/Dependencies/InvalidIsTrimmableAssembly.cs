using System.Reflection;

[assembly: AssemblyMetadata ("IsTrimmable", "True")]
[assembly: AssemblyMetadata ("IsTrimmable", "False")]
[assembly: AssemblyMetadata ("IsTrimmable", "True")]
[assembly: AssemblyMetadata ("IsTrimmable", "true")]

namespace Mono.Linker.Tests.Cases.CoreLink.Dependencies
{
	public static class InvalidIsTrimmableAssembly
	{
		public static void Used ()
		{
		}

		public static void Unused ()
		{
		}
	}
}