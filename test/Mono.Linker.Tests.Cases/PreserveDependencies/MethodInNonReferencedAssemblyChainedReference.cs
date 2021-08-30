using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Expectations.Assertions;
using Mono.Linker.Tests.Cases.Expectations.Metadata;
using Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies;

namespace Mono.Linker.Tests.Cases.PreserveDependencies
{
	[SetupCompileBefore ("FakeSystemAssembly.dll", new[] { "Dependencies/PreserveDependencyAttribute.cs" })]
	[SetupCompileBefore ("base.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase.cs" })]
	[SetupCompileBefore ("base2.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyBase2.cs" }, references: new[] { "base.dll" }, addAsReference: false)]
	[SetupCompileBefore ("reference.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyChainedLibrary.cs" }, references: new[] { "base.dll", "FakeSystemAssembly.dll" }, addAsReference: false)]
	[SetupCompileBefore ("library.dll", new[] { "Dependencies/MethodInNonReferencedAssemblyChainedReferenceLibrary.cs" }, references: new[] { "base.dll", "reference.dll" }, addAsReference: false)]
	[KeptAssembly ("base.dll")]
	[KeptAssembly ("base2.dll")]
	[KeptAssembly ("library.dll")]
	[KeptAssembly ("reference.dll")]
	[KeptMemberInAssembly ("base.dll", typeof (MethodInNonReferencedAssemblyBase), "Method()")]
	[KeptMemberInAssembly ("base2.dll", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.MethodInNonReferencedAssemblyBase2", "Method()")]
	[KeptMemberInAssembly ("library.dll", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.MethodInNonReferencedAssemblyChainedReferenceLibrary", "Method()")]
	[KeptMemberInAssembly ("reference.dll", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.MethodInNonReferencedAssemblyChainedLibrary", "Method()")]
	public class MethodInNonReferencedAssemblyChainedReference
	{
		public static void Main ()
		{
			var obj = new Foo ();
			var val = obj.Method ();
			Dependency ();
		}

		[Kept]
		[PreserveDependency (".ctor()", "Mono.Linker.Tests.Cases.PreserveDependencies.Dependencies.MethodInNonReferencedAssemblyChainedReferenceLibrary", "library")]
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