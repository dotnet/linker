namespace Mono.Linker
{
	public static class PlatformAssemblies
	{
#if NETCOREAPP
		public const string CoreLib = "System.Private.CoreLib";
#else
		public const string CoreLib = "mscorlib";
#endif
	}
}