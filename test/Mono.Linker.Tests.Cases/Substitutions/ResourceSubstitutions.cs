using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.Substitutions
{
	[SetupLinkerSubstitutionFile ("ResourceSubstitutions.xml")]
	[SetupCompileResource ("Dependencies/ResourceFile.txt", "ResourceFile.txt")]
	[LogContains ("IL2033: Missing 'name' attribute for resource")]
	[LogContains ("IL2034: Invalid 'action' attribute for resource 'ResourceFile.txt'")]
	[LogContains ("IL2035: Could not find embedded resource 'MissingResourceFile.txt' to remove in assembly 'test'")]
	[RemovedResourceInAssembly ("test.exe", "ResourceFile.txt")]
	public class ResourceSubstitutions
	{
		public static void Main ()
		{
		}
	}
}