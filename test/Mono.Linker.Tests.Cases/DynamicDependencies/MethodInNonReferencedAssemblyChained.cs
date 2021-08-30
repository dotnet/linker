using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;

namespace Mono.Linker.Tests.Cases.DynamicDependencies
{
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore ("base2.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase2.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyChainedLibrary.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[KeptAssembly ("base2.dll")]
	[KeptAssembly ("library.dll")]
	[KeptMemberInAssembly ("base.dll", typeof (MethodInNonReferencedAssemblyBase), "Method()")]
	[KeptMemberInAssembly ("base2.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyBase2", "Method()")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyChainedLibrary", "Method()")]
	public class MethodInNonReferencedAssemblyChained
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
			Dependency ();
		}

		[Kept]
		[DynamicDependency ("#ctor()", "Mono.Linker.Tests.Cases.DynamicDependencies.Dependencies.MethodInNonReferencedAssemblyChainedLibrary", "library")]
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