using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyLibrary.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[RemovedAssembly ("library.dll")]
	[KeptMemberInAssembly ("base.dll", typeof (MethodInNonReferencedAssemblyBase), "Method()")]
	public class OnUnusedMethodInNonReferencedAssembly
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
		}

		[DynamicDependency (".ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyLibrary", "library")]
		static void Dependency ()
		{
		}

		[Kept]
		[KeptMember (".ctor()")]
		[KeptBaseType (typeof (MethodInNonReferencedAssemblyBase))]
		class Foo : MethodInNonReferencedAssemblyBase
		{
			[Kept]
			public override string Method ()
			{
				return "Foo";
			}
		}
	}
}