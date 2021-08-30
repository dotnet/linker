using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupLinkerDefaultAction ("copyused")]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/OnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib.cs" }, addAsReference: false)]
	[RemovedAssembly ("library.dll")]
	public class OnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction
	{
#if NETCOREAPP
		[Kept]
		public OnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction ()
		{
		}
#endif

		public static void Main ()
		{
		}

		[DynamicDependency ("MethodPreservedViaDependencyAttribute()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.OnUnusedMethodInNonReferencedAssemblyWithCopyUsedAction_Lib", "library")]
#if NETCOREAPP
		[Kept]
		[KeptAttributeAttribute (typeof (DynamicDependencyAttribute))]
#endif
		static void Dependency ()
		{
		}
	}
}